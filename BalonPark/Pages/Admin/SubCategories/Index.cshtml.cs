using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.SubCategories;

public class IndexModel : BaseAdminPage
{
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public IndexModel(SubCategoryRepository subCategoryRepository, CategoryRepository categoryRepository, ICacheService cacheService)
    {
        _subCategoryRepository = subCategoryRepository;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
    }

    public List<SubCategory> SubCategories { get; set; } = new();
    public new List<Category> Categories { get; set; } = new();
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
        
        // Debug bilgisi
        Console.WriteLine($"SubCategories Count: {SubCategories.Count}");
        Console.WriteLine($"Categories Count: {Categories.Count}");
        
        foreach (var subCategory in SubCategories)
        {
            Console.WriteLine($"SubCategory: {subCategory.Name} - CategoryId: {subCategory.CategoryId}");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var subCategory = await _subCategoryRepository.GetByIdAsync(id);
            await _subCategoryRepository.DeleteAsync(id);
            
            // Cache'i temizle
            await _cacheService.InvalidateSubCategoriesAsync();
            await _cacheService.InvalidateSubCategoryAsync(id);
            if (subCategory != null)
            {
                await _cacheService.InvalidateSubCategoryBySlugAsync(subCategory.Slug);
            }
            
            SuccessMessage = "Alt kategori başarıyla silindi!";
        }
        catch
        {
            SuccessMessage = "Alt kategori silinirken hata oluştu!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearCacheAsync()
    {
        try
        {
            // Tüm cache'i temizle
            await _cacheService.InvalidateAllAsync();
            SuccessMessage = "Cache başarıyla temizlendi!";
        }
        catch (Exception ex)
        {
            SuccessMessage = $"Cache temizlenirken hata oluştu: {ex.Message}";
        }

        return RedirectToPage();
    }
}

