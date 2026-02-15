using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class ProductsModel : BasePage
{
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CurrencyService _currencyService;
    private readonly IYandexExchangeRateService _yandexExchangeRateService;

    public SubCategory? SubCategory { get; set; }
    public string CategorySlug { get; set; } = string.Empty;
    public List<ProductWithImage> Products { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50; // Her sayfada 50 ürün
    public int TotalProducts { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);

    public ProductsModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService,
        IYandexExchangeRateService yandexExchangeRateService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _subCategoryRepository = subCategoryRepository;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
        _yandexExchangeRateService = yandexExchangeRateService;
    }

    public async Task<IActionResult> OnGetAsync(string categorySlug, string subCategorySlug)
    {
        if (string.IsNullOrEmpty(categorySlug) || string.IsNullOrEmpty(subCategorySlug))
            return NotFound();

        // Currency artık BasePage'den geliyor, cookie'den okunuyor
        CategorySlug = categorySlug;
        
        // PageNumber parametresi BindProperty ile otomatik alınıyor
        CurrentPage = PageNumber > 0 ? PageNumber : 1;

        // Alt kategoriyi slug ile al
        SubCategory = await _subCategoryRepository.GetBySlugAsync(subCategorySlug);

        if (SubCategory == null)
            return NotFound();

        // Alt kategoriye ait ürünleri al
        var products = await _productRepository.GetBySubCategorySlugAsync(subCategorySlug);
        
        TotalProducts = products.Count();
        
        // Pagination için ürünleri al
        var pagedProducts = products
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var tryToRub = await _yandexExchangeRateService.GetTryToRubRateAsync();
        foreach (var product in pagedProducts)
        {
            var mainImage = await _productImageRepository.GetMainImageAsync(product.Id);
            var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
            product.UsdPrice = Math.Round(usdPrice, 2);
            product.EuroPrice = Math.Round(euroPrice, 2);
            product.RubPrice = Math.Round(product.Price * tryToRub, 2);
            Products.Add(new ProductWithImage
            {
                Product = product,
                MainImage = mainImage
            });
        }

        ViewData["ActiveCategorySlug"] = CategorySlug;
        ViewData["ActiveSubCategorySlug"] = SubCategory?.Slug;

        return Page();
    }
}

