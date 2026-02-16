using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class ProductDetailModel : BasePage
{
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CurrencyService _currencyService;
    private readonly IYandexExchangeRateService _yandexExchangeRateService;
    private readonly PdfService _pdfService;
    public Product? Product { get; set; }
    public ProductImage? MainImage { get; set; }
    public List<ProductImage> ProductImages { get; set; } = new();
    /// <summary>İlgili ürünler (aynı alt kategoriden, mevcut ürün hariç).</summary>
    public List<ProductWithImage> RelatedProducts { get; set; } = new();
    /// <summary>Bu hafta popüler (görüntülenme / IsPopular, mevcut ürün hariç).</summary>
    public List<ProductWithImage> PopularProducts { get; set; } = new();

    public ProductDetailModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService,
        IYandexExchangeRateService yandexExchangeRateService,
        PdfService pdfService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
        _yandexExchangeRateService = yandexExchangeRateService;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> OnGetAsync(string categorySlug, string subCategorySlug, string productSlug)
    {
        // Google Shopping gereksinimleri: Ürün sayfası her zaman doğru yüklenmeli
        if (string.IsNullOrEmpty(productSlug))
            return NotFound();

        // Ürünü slug ile al
        Product = await _productRepository.GetBySlugAsync(productSlug);

        // Google Shopping gereksinimleri: Ürün bulunamazsa 404 döndür (ana sayfaya yönlendirme yapma)
        if (Product == null)
            return NotFound();

        // URL doğrulama (SEO ve Google Shopping için önemli)
        // Google, ürün sayfasının doğru URL'de yüklendiğini kontrol eder
        if (Product.CategorySlug != categorySlug || Product.SubCategorySlug != subCategorySlug)
        {
            // Doğru URL'e 301 redirect (kalıcı yönlendirme - SEO için önemli)
            return RedirectToPagePermanent("/ProductDetail", new 
            { 
                categorySlug = Product.CategorySlug, 
                subCategorySlug = Product.SubCategorySlug, 
                productSlug = Product.Slug 
            });
        }

        // Görüntülenme sayısını artır
        var updatedViewCount = await _productRepository.IncrementViewCountAsync(
            Product.Id,
            Product.Slug,
            Product.CategorySlug,
            Product.SubCategorySlug);
        Product.ViewCount = updatedViewCount;

        // Ürün resimlerini al
        var images = await _productImageRepository.GetByProductIdAsync(Product.Id);
        ProductImages = images.ToList();

        // Ana resmi bul
        MainImage = ProductImages.FirstOrDefault(i => i.IsMainImage) ?? ProductImages.FirstOrDefault();

        // USD, Euro ve RUB fiyatlarını hesapla
        var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(Product.Price);
        var tryToRub = await _yandexExchangeRateService.GetTryToRubRateAsync();
        Product.UsdPrice = Math.Round(usdPrice, 2);
        Product.EuroPrice = Math.Round(euroPrice, 2);
        Product.RubPrice = Math.Round(Product.Price * tryToRub, 2);

        // İlgili ürünler (aynı alt kategoriden)
        var related = await _productRepository.GetRelatedBySubCategoryAsync(Product.SubCategorySlug ?? "", Product.Id, 6);
        foreach (var p in related)
        {
            var (pu, pe) = await _currencyService.CalculatePricesAsync(p.Price);
            p.UsdPrice = Math.Round(pu, 2);
            p.EuroPrice = Math.Round(pe, 2);
            p.RubPrice = Math.Round(p.Price * tryToRub, 2);
            var mainImg = await _productImageRepository.GetMainImageAsync(p.Id);
            RelatedProducts.Add(new ProductWithImage { Product = p, MainImage = mainImg });
        }

        ViewData["ActiveCategorySlug"] = Product.CategorySlug;
        ViewData["ActiveSubCategorySlug"] = Product.SubCategorySlug;
        ViewData["IsAdminLoggedIn"] = HttpContext.Session.IsAdminLoggedIn();

        // Popüler ürünler (görüntülenme / IsPopular)
        var popular = await _productRepository.GetPopularProductsAsync(Product.Id, 6);
        foreach (var p in popular)
        {
            var (pu, pe) = await _currencyService.CalculatePricesAsync(p.Price);
            p.UsdPrice = Math.Round(pu, 2);
            p.EuroPrice = Math.Round(pe, 2);
            p.RubPrice = Math.Round(p.Price * tryToRub, 2);
            var mainImg = await _productImageRepository.GetMainImageAsync(p.Id);
            PopularProducts.Add(new ProductWithImage { Product = p, MainImage = mainImg });
        }

        // Google Shopping gereksinimleri: Sayfa başarıyla yüklendi
        return Page();
    }

    /// <summary>
    /// Sayfadaki ürünün kendisini PDF olarak indirir (public ürün detay sayfası).
    /// </summary>
    public async Task<IActionResult> OnGetExportRelatedProductsPdfAsync(string categorySlug, string subCategorySlug, string productSlug)
    {
        if (string.IsNullOrEmpty(productSlug))
            return NotFound();

        var product = await _productRepository.GetBySlugAsync(productSlug);
        if (product == null)
            return NotFound();

        if (product.CategorySlug != categorySlug || product.SubCategorySlug != subCategorySlug)
            return RedirectToPagePermanent("/ProductDetail", new { categorySlug = product.CategorySlug, subCategorySlug = product.SubCategorySlug, productSlug = product.Slug });

        var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
        product.UsdPrice = Math.Round(usdPrice, 2);
        product.EuroPrice = Math.Round(euroPrice, 2);

        var mainImage = await _productImageRepository.GetMainImageAsync(product.Id);
        var allImages = (await _productImageRepository.GetByProductIdAsync(product.Id)).ToList();

        var pdfBytes = await _pdfService.GenerateProductDetailPdfAsync(product, mainImage, allImages);
        var fileName = $"Urun-{product.Slug}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}

