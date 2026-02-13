using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class CategoryModel : BasePage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly CurrencyService _currencyService;

    public Category? Category { get; set; }
    public List<SubCategory> SubCategories { get; set; } = new();
    public Dictionary<string, List<ProductWithImage>> ProductsBySubCategory { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int MaxProductsPerSubCategory { get; set; } = 36; // Her alt kategoride max 36 ürün
    public int TotalProducts { get; set; }
    public int TotalPages => 1; // Ana kategoride sayfalama yok, hepsi gösterilir

    public CategoryModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CurrencyService currencyService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        // Currency artık BasePage'den geliyor, cookie'den okunuyor
        CurrentPage = 1; // Ana kategoride pagination yok
        
        if (string.IsNullOrEmpty(slug))
            return NotFound();

        Category = await _categoryRepository.GetBySlugAsync(slug);

        if (Category == null)
            return NotFound();

        var subCategories = await _subCategoryRepository.GetByCategorySlugAsync(slug);
        SubCategories = subCategories.ToList();

        // Load ALL products for this category
        var allProducts = await _productRepository.GetByCategorySlugAsync(slug);
        
        TotalProducts = allProducts.Count();
        
        // Group products by subcategory and limit each group to MaxProductsPerSubCategory
        ProductsBySubCategory = new Dictionary<string, List<ProductWithImage>>();
        
        var groupedProducts = allProducts.GroupBy(p => p.SubCategorySlug ?? "");
        
        foreach (var group in groupedProducts)
        {
            var productsWithImages = new List<ProductWithImage>();
            
            // Her alt kategoriden maksimum MaxProductsPerSubCategory kadar ürün al
            var productsToShow = group.Take(MaxProductsPerSubCategory);
            
            foreach (var product in productsToShow)
            {
                var mainImage = await _productImageRepository.GetMainImageAsync(product.Id);
                
                // USD ve Euro fiyatlarını hesapla
                var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
                
                // Ürünün fiyat bilgilerini güncelle
                product.UsdPrice = Math.Round(usdPrice, 2);
                product.EuroPrice = Math.Round(euroPrice, 2);
                
                productsWithImages.Add(new ProductWithImage
                {
                    Product = product,
                    MainImage = mainImage
                });
            }
            
            ProductsBySubCategory[group.Key] = productsWithImages;
        }

        ViewData["ActiveCategorySlug"] = slug;
        ViewData["ActiveSubCategorySlug"] = null;

        return Page();
    }
}

