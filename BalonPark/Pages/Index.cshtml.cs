using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class IndexModel(
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    SettingsRepository settingsRepository,
    ProductRepository productRepository,
    ProductImageRepository productImageRepository,
    CurrencyService currencyService,
    IUrlService urlService,
    ICurrencyCookieService currencyCookieService)
    : BasePage(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
{
    public List<ProductWithImage> Products { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 100; // Her sayfada 100 ürün
    public int TotalProducts { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);

    public async Task OnGetAsync()
    {
        // Currency artık BasePage'den geliyor, cookie'den okunuyor
        
        // PageNumber parametresi BindProperty ile otomatik alınıyor
        CurrentPage = PageNumber > 0 ? PageNumber : 1;
        
        // Ürünleri çek
        var products = await productRepository.GetAllAsync();
        var activeProducts = products.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id).ToList();
        
        // Toplam ürün sayısı
        TotalProducts = activeProducts.Count;
        
        // Pagination için ürünleri al
        var pagedProducts = activeProducts
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Her ürün için ana resmi çek ve fiyatları hesapla
        foreach (var product in pagedProducts)
        {
            var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
            
            // USD ve Euro fiyatlarını hesapla
            var (usdPrice, euroPrice) = await currencyService.CalculatePricesAsync(product.Price);
            
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
}