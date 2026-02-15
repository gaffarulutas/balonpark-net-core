using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class ProductListModel : BasePage
{
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly CurrencyService _currencyService;
    private readonly IYandexExchangeRateService _yandexExchangeRateService;

    public List<ProductWithImage> Products { get; set; } = [];
    public List<Category> FilterCategories { get; set; } = [];
    public List<SubCategory> FilterSubCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SubCategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ProductNameFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 24;
    public int TotalProducts { get; set; }
    public int TotalPages { get; set; }

    public ProductListModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService,
        IYandexExchangeRateService yandexExchangeRateService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _currencyService = currencyService;
        _yandexExchangeRateService = yandexExchangeRateService;
    }

    public string GetFilterQueryString(int? pageNumber = null)
    {
        var q = new List<string>();
        if (pageNumber.HasValue && pageNumber.Value > 1)
            q.Add($"PageNumber={pageNumber.Value}");
        if (CategoryId.HasValue && CategoryId.Value > 0)
            q.Add($"CategoryId={CategoryId.Value}");
        if (SubCategoryId.HasValue && SubCategoryId.Value > 0)
            q.Add($"SubCategoryId={SubCategoryId.Value}");
        if (!string.IsNullOrWhiteSpace(ProductNameFilter))
            q.Add($"ProductNameFilter={Uri.EscapeDataString(ProductNameFilter)}");
        if (!string.IsNullOrWhiteSpace(SortBy))
            q.Add($"SortBy={Uri.EscapeDataString(SortBy)}");
        return q.Count > 0 ? "?" + string.Join("&", q) : "";
    }

    public async Task OnGetAsync()
    {
        FilterCategories = (await _categoryRepository.GetAllAsync()).Where(c => c.IsActive).ToList();
        if (CategoryId.HasValue && CategoryId.Value > 0)
            FilterSubCategories = (await _subCategoryRepository.GetByCategoryIdAsync(CategoryId.Value)).Where(sc => sc.IsActive).ToList();
        else
            FilterSubCategories = (await _subCategoryRepository.GetAllAsync()).Where(sc => sc.IsActive).ToList();

        var allProducts = (await _productRepository.GetAllAsync())
            .Where(p => p.IsActive)
            .ToList();

        if (CategoryId.HasValue && CategoryId.Value > 0)
            allProducts = allProducts.Where(p => p.CategoryId == CategoryId.Value).ToList();

        if (SubCategoryId.HasValue && SubCategoryId.Value > 0)
            allProducts = allProducts.Where(p => p.SubCategoryId == SubCategoryId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(ProductNameFilter))
        {
            allProducts = allProducts
                .Where(p => p.Name.Contains(ProductNameFilter, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        allProducts = SortBy switch
        {
            "name" => allProducts.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            "nameDesc" => allProducts.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            "price" => allProducts.OrderBy(p => p.Price).ToList(),
            "priceDesc" => allProducts.OrderByDescending(p => p.Price).ToList(),
            "oldest" => allProducts.OrderBy(p => p.CreatedAt).ToList(),
            "newest" => allProducts.OrderByDescending(p => p.CreatedAt).ToList(),
            "displayOrder" or _ => allProducts.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id).ToList()
        };

        TotalProducts = allProducts.Count;
        TotalPages = (int)Math.Ceiling(TotalProducts / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

        var paged = allProducts
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var tryToRub = await _yandexExchangeRateService.GetTryToRubRateAsync();
        foreach (var product in paged)
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

        // SEO: Pagination prev/next for crawlers (2026 best practice)
        var basePath = "/products";
        if (TotalPages > 1)
        {
            if (PageNumber > 1)
                ViewData["PrevUrl"] = _urlService.GetPageUrl(basePath + GetFilterQueryString(PageNumber - 1));
            if (PageNumber < TotalPages)
                ViewData["NextUrl"] = _urlService.GetPageUrl(basePath + GetFilterQueryString(PageNumber + 1));
        }

        // SEO: CollectionPage + BreadcrumbList + ItemList JSON-LD (2026 best practice)
        var pageUrl = _urlService.GetPageUrl(basePath + GetFilterQueryString());
        var baseUrl = _urlService.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl) && HttpContext?.Request != null)
            baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        var companyName = SiteSettings?.CompanyName ?? "Balon Park Şişme Oyun Grupları";
        var pageName = "Ürünler";
        var pageDescription = $"Tüm şişme oyun parkları, kaydıraklar, havuzlar ve çocuk oyun grupları. {companyName} ürün kataloğu.";

        var itemListElements = new List<Dictionary<string, object>>();
        var take = Math.Min(Products.Count, 20);
        for (var i = 0; i < take; i++)
        {
            var item = Products[i];
            var p = item.Product;
            var productUrl = _urlService.GetProductUrl(p.CategorySlug ?? "", p.SubCategorySlug ?? "", p.Slug);
            if (!productUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                productUrl = baseUrl?.TrimEnd('/') + productUrl;
            var imagePath = item.MainImage?.LargePath ?? item.MainImage?.ThumbnailPath ?? "/assets/images/no-image.png";
            var imageUrl = _urlService.GetImageUrl(imagePath);
            itemListElements.Add(new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["item"] = new Dictionary<string, object>
                {
                    ["@type"] = "Product",
                    ["name"] = p.Name ?? "",
                    ["description"] = string.IsNullOrEmpty(p.Description) ? (p.Name ?? "") : p.Description,
                    ["url"] = productUrl,
                    ["image"] = imageUrl,
                    ["offers"] = new Dictionary<string, object>
                    {
                        ["@type"] = "Offer",
                        ["price"] = p.Price.ToString("F0", System.Globalization.CultureInfo.InvariantCulture),
                        ["priceCurrency"] = "TRY",
                        ["availability"] = p.Stock > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock"
                    }
                }
            });
        }

        var structuredData = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CollectionPage",
            ["name"] = $"{pageName} | {companyName}",
            ["description"] = pageDescription,
            ["url"] = pageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pageUrl : (baseUrl?.TrimEnd('/') + pageUrl),
            ["mainEntity"] = new Dictionary<string, object>
            {
                ["@type"] = "ItemList",
                ["name"] = $"{pageName} - Ürün Kataloğu",
                ["numberOfItems"] = TotalProducts,
                ["itemListElement"] = itemListElements
            },
            ["breadcrumb"] = new Dictionary<string, object>
            {
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new List<Dictionary<string, object>>
                {
                    new() { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "Ana Sayfa", ["item"] = _urlService.GetPageUrl("/") },
                    new() { ["@type"] = "ListItem", ["position"] = 2, ["name"] = pageName, ["item"] = pageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pageUrl : (baseUrl?.TrimEnd('/') + pageUrl) }
                }
            }
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null, WriteIndented = false };
        ViewData["StructuredData"] = JsonSerializer.Serialize(structuredData, jsonOptions);
    }
}
