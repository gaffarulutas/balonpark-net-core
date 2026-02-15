using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class CompareModel : BasePage
{
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CurrencyService _currencyService;
    private readonly IYandexExchangeRateService _yandexExchangeRateService;

    public CompareModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService,
        IYandexExchangeRateService yandexExchangeRateService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
        _yandexExchangeRateService = yandexExchangeRateService;
    }

    public List<ProductWithImage> Products { get; set; } = new();
    public string ProductSlugs { get; set; } = string.Empty;

    public async Task OnGetAsync(string? slugs)
    {
        if (string.IsNullOrWhiteSpace(slugs))
            return;

        ProductSlugs = slugs;
        var slugList = slugs.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (slugList.Count == 0)
            return;

        // Tek sorguda ürünleri getir (N+1 önlemi)
        var productsBySlug = (await _productRepository.GetBySlugsAsync(slugList)).ToList();
        if (productsBySlug.Count == 0)
            return;

        var productIds = productsBySlug.Select(p => p.Id).ToList();
        var mainImages = await _productImageRepository.GetMainImagesByProductIdsAsync(productIds);
        var tryToRub = await _yandexExchangeRateService.GetTryToRubRateAsync();

        foreach (var product in productsBySlug)
        {
            var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
            product.UsdPrice = Math.Round(usdPrice, 2);
            product.EuroPrice = Math.Round(euroPrice, 2);
            product.RubPrice = Math.Round(product.Price * tryToRub, 2);
            mainImages.TryGetValue(product.Id, out var mainImage);
            Products.Add(new ProductWithImage { Product = product, MainImage = mainImage });
        }
    }

    public class ProductWithImage
    {
        public Product Product { get; set; } = new();
        public ProductImage? MainImage { get; set; }
    }
}

