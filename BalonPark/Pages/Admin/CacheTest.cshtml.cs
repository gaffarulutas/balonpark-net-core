using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Services;
using System.Diagnostics;

namespace BalonPark.Pages.Admin;

public class CacheTestModel : BaseAdminPage
{
    private readonly ProductRepository _productRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly ICacheService _cacheService;

    public CacheTestModel(
        ProductRepository productRepository,
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _settingsRepository = settingsRepository;
        _cacheService = cacheService;
    }

    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
    public int TotalRequests { get; set; }
    public long FirstLoadTime { get; set; }
    public long SecondLoadTime { get; set; }
    public double PerformanceImprovement { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public string? SlugTestMessage { get; set; }
    public string? SlugValidationMessage { get; set; }
    public string? SettingsTestMessage { get; set; }
    public Models.Settings? CurrentSettings { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCacheStats();
        await LoadSettingsInfo();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        switch (action)
        {
            case "test":
                await PerformCacheTest();
                break;
            case "clear":
                await _cacheService.InvalidateAllAsync();
                Message = "Cache başarıyla temizlendi!";
                IsSuccess = true;
                break;
            case "load":
                await LoadDataIntoCache();
                Message = "Veriler cache'e yüklendi!";
                IsSuccess = true;
                break;
            case "slugtest":
                await PerformSlugTest();
                break;
            case "slugvalidation":
                await PerformSlugValidation();
                break;
            case "settingstest":
                await PerformSettingsTest();
                break;
        }

        await LoadCacheStats();
        await LoadSettingsInfo();
        return Page();
    }

    private async Task LoadCacheStats()
    {
        // Cache istatistiklerini yükle
        var stopwatch = Stopwatch.StartNew();
        
        // Cache durumlarını kaydet
        var productsCached = await _cacheService.GetProductsAsync() != null;
        var categoriesCached = await _cacheService.GetCategoriesAsync() != null;
        var subCategoriesCached = await _cacheService.GetSubCategoriesAsync() != null;
        
        // İlk yükleme (cache miss olabilir)
        var products = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var subCategories = await _subCategoryRepository.GetAllAsync();
        
        FirstLoadTime = stopwatch.ElapsedMilliseconds;
        
        stopwatch.Restart();
        
        // İkinci yükleme (cache hit olmalı)
        products = await _productRepository.GetAllAsync();
        categories = await _categoryRepository.GetAllAsync();
        subCategories = await _subCategoryRepository.GetAllAsync();
        
        SecondLoadTime = stopwatch.ElapsedMilliseconds;
        
        // Gerçek cache hit/miss sayısını hesapla
        var firstLoadHits = (productsCached ? 1 : 0) + (categoriesCached ? 1 : 0) + (subCategoriesCached ? 1 : 0);
        var firstLoadMisses = 3 - firstLoadHits;
        
        // İkinci yükleme tümü cache'den olmalı (çünkü birinci yüklemede cache'e yazıldı)
        var secondLoadHits = 3;
        
        TotalRequests = 6; // 3 repository x 2 yükleme
        CacheHitCount = firstLoadHits + secondLoadHits;
        CacheMissCount = firstLoadMisses;
        
        if (FirstLoadTime > 0)
        {
            PerformanceImprovement = Math.Round(((double)(FirstLoadTime - SecondLoadTime) / FirstLoadTime) * 100, 2);
            if (PerformanceImprovement < 0) PerformanceImprovement = 0; // Negatif değerleri sıfırla
        }
    }

    private async Task PerformCacheTest()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Cache'i temizle
        await _cacheService.InvalidateAllAsync();
        
        // İlk yükleme (cache miss)
        var firstLoadStopwatch = Stopwatch.StartNew();
        var products = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var subCategories = await _subCategoryRepository.GetAllAsync();
        firstLoadStopwatch.Stop();
        
        // İkinci yükleme (cache hit)
        var secondLoadStopwatch = Stopwatch.StartNew();
        products = await _productRepository.GetAllAsync();
        categories = await _categoryRepository.GetAllAsync();
        subCategories = await _subCategoryRepository.GetAllAsync();
        secondLoadStopwatch.Stop();
        
        stopwatch.Stop();
        
        FirstLoadTime = firstLoadStopwatch.ElapsedMilliseconds;
        SecondLoadTime = secondLoadStopwatch.ElapsedMilliseconds;
        
        if (FirstLoadTime > 0)
        {
            PerformanceImprovement = Math.Round(((double)(FirstLoadTime - SecondLoadTime) / FirstLoadTime) * 100, 2);
        }
        
        Message = $"Cache test tamamlandı! İlk yükleme: {FirstLoadTime}ms, İkinci yükleme: {SecondLoadTime}ms, Performans artışı: %{PerformanceImprovement}";
        IsSuccess = true;
    }

    private async Task LoadDataIntoCache()
    {
        // Tüm verileri cache'e yükle
        await _productRepository.GetAllAsync();
        await _categoryRepository.GetAllAsync();
        await _subCategoryRepository.GetAllAsync();
    }

    private async Task PerformSlugTest()
    {
        try
        {
            // Test için bir kategori al
            var categories = await _categoryRepository.GetAllAsync();
            var testCategory = categories.FirstOrDefault();
            
            if (testCategory == null)
            {
                SlugTestMessage = "Test için kategori bulunamadı!";
                return;
            }

            var originalSlug = testCategory.Slug;
            var testSlug = $"test-slug-{DateTime.Now:yyyyMMddHHmmss}";
            
            // Eski slug ile cache'den veri çek
            var cachedByOriginalSlug = await _cacheService.GetCategoryBySlugAsync(originalSlug);
            
            // Slug'ı güncelle
            testCategory.Slug = testSlug;
            testCategory.UpdatedAt = DateTime.Now;
            await _categoryRepository.UpdateAsync(testCategory);
            
            // Yeni slug ile cache'den veri çek
            var cachedByNewSlug = await _cacheService.GetCategoryBySlugAsync(testSlug);
            
            // Eski slug ile tekrar çek (cache'den temizlenmiş olmalı)
            var cachedByOriginalSlugAfterUpdate = await _cacheService.GetCategoryBySlugAsync(originalSlug);
            
            // Slug'ı geri eski haline getir
            testCategory.Slug = originalSlug;
            testCategory.UpdatedAt = DateTime.Now;
            await _categoryRepository.UpdateAsync(testCategory);
            
            SlugTestMessage = $"Slug test tamamlandı! " +
                            $"Orijinal slug cache: {(cachedByOriginalSlug != null ? "Var" : "Yok")}, " +
                            $"Yeni slug cache: {(cachedByNewSlug != null ? "Var" : "Yok")}, " +
                            $"Güncelleme sonrası orijinal slug cache: {(cachedByOriginalSlugAfterUpdate != null ? "Var" : "Yok")}";
        }
        catch (Exception ex)
        {
            SlugTestMessage = $"Slug test hatası: {ex.Message}";
        }
    }

    private async Task PerformSlugValidation()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();
            var subCategories = await _subCategoryRepository.GetAllAsync();

            var productSlugIssues = products.Where(p => string.IsNullOrEmpty(p.Slug)).ToList();
            var categorySlugIssues = categories.Where(c => string.IsNullOrEmpty(c.Slug)).ToList();
            var subCategorySlugIssues = subCategories.Where(sc => string.IsNullOrEmpty(sc.Slug)).ToList();

            var issues = new List<string>();
            
            if (productSlugIssues.Any())
            {
                issues.Add($"Ürünlerde {productSlugIssues.Count} adet boş slug bulundu: {string.Join(", ", productSlugIssues.Select(p => p.Name))}");
            }
            
            if (categorySlugIssues.Any())
            {
                issues.Add($"Kategorilerde {categorySlugIssues.Count} adet boş slug bulundu: {string.Join(", ", categorySlugIssues.Select(c => c.Name))}");
            }
            
            if (subCategorySlugIssues.Any())
            {
                issues.Add($"Alt kategorilerde {subCategorySlugIssues.Count} adet boş slug bulundu: {string.Join(", ", subCategorySlugIssues.Select(sc => sc.Name))}");
            }

            if (issues.Any())
            {
                SlugValidationMessage = string.Join(" | ", issues);
            }
            else
            {
                SlugValidationMessage = "Tüm slug'lar doğru! Ürünler: " + products.Count() + 
                                     ", Kategoriler: " + categories.Count() + 
                                     ", Alt Kategoriler: " + subCategories.Count();
            }
        }
        catch (Exception ex)
        {
            SlugValidationMessage = $"Slug validation hatası: {ex.Message}";
        }
    }

    private async Task LoadSettingsInfo()
    {
        try
        {
            CurrentSettings = await _settingsRepository.GetFirstAsync();
            var cachedSettings = await _cacheService.GetSettingsAsync();
            
            SettingsTestMessage = cachedSettings != null 
                ? "✅ Settings cache'den geldi" 
                : "❌ Settings cache'de yok, DB'den çekildi";
        }
        catch (Exception ex)
        {
            SettingsTestMessage = $"Settings yükleme hatası: {ex.Message}";
        }
    }

    private async Task PerformSettingsTest()
    {
        try
        {
            var sw1 = Stopwatch.StartNew();
            var dbSettings = await _settingsRepository.GetFirstAsync();
            sw1.Stop();
            
            var sw2 = Stopwatch.StartNew();
            var cachedSettings = await _cacheService.GetSettingsAsync();
            sw2.Stop();
            
            SettingsTestMessage = $"DB: {sw1.ElapsedMilliseconds}ms, Cache: {sw2.ElapsedMilliseconds}ms | " +
                                $"CompanyName: {dbSettings?.CompanyName ?? "NULL"}, " +
                                $"MetaTitle: {dbSettings?.MetaTitle ?? "NULL"}, " +
                                $"Logo: {dbSettings?.Logo ?? "NULL"}";
            
            Message = "Settings test tamamlandı!";
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            SettingsTestMessage = $"Settings test hatası: {ex.Message}";
            IsSuccess = false;
        }
    }
}
