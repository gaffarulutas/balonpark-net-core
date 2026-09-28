using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Pages.Admin.Categories;

public class IndexModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ICatalogSyncPublisher _catalogSync;

    public IndexModel(CategoryRepository categoryRepository, ICacheService cacheService, ICatalogSyncPublisher catalogSync)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _catalogSync = catalogSync;
    }

    public new List<Category> Categories { get; set; } = new();
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            await _categoryRepository.DeleteAsync(id);
            
            await _cacheService.InvalidateCategoriesAsync();
            await _cacheService.InvalidateCategoryAsync(id);
            if (category != null)
            {
                await _cacheService.InvalidateCategoryBySlugAsync(category.Slug);
            }
            _catalogSync.PublishDelete(CatalogSyncEntityType.Category, id);
            
            SuccessMessage = "Kategori başarıyla silindi!";
        }
        catch
        {
            SuccessMessage = "Kategori silinirken hata oluştu!";
        }

        return RedirectToPage();
    }
}
