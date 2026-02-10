using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class CompareModel : BasePage
{
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CurrencyService _currencyService;

    public CompareModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
    }

    public List<ProductWithImage> Products { get; set; } = new();
    public string ProductSlugs { get; set; } = string.Empty;

    public async Task OnGetAsync(string? slugs)
    {

        if (string.IsNullOrWhiteSpace(slugs))
        {
            // Slug yoksa boş liste
            return;
        }

        ProductSlugs = slugs;

        // Slug'ları ayır (: ile ayrılmış)
        var slugList = slugs.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Her slug için ürünü getir
        foreach (var slug in slugList)
        {
            var product = await _productRepository.GetBySlugAsync(slug);
            if (product == null) continue;

            var mainImage = await _productImageRepository.GetMainImageAsync(product.Id);

            // USD ve Euro fiyatlarını hesapla
            var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
            
            // Ürünün fiyat bilgilerini güncelle
            product.UsdPrice = Math.Round(usdPrice, 2);
            product.EuroPrice = Math.Round(euroPrice, 2);

            Products.Add(new ProductWithImage
            {
                Product = product,
                MainImage = mainImage
            });
        }
    }

    public class ProductWithImage
    {
        public Product Product { get; set; } = new();
        public ProductImage? MainImage { get; set; }
    }
}

