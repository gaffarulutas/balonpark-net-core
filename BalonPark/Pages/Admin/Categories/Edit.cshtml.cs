using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Pages.Admin.Categories;

public class EditModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ICatalogSyncPublisher _catalogSync;

    public EditModel(CategoryRepository categoryRepository, ICacheService cacheService, ICatalogSyncPublisher catalogSync)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _catalogSync = catalogSync;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        
        if (category == null)
        {
            return RedirectToPage("./Index");
        }

        Category = category;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Category.Name))
        {
            ModelState.AddModelError("Category.Name", "Kategori adı gereklidir.");
            return Page();
        }

        Category.Slug = SlugHelper.GenerateSlug(Category.Name);
        Category.UpdatedAt = DateTime.Now;
        await _categoryRepository.UpdateAsync(Category);

        await _cacheService.InvalidateCategoriesAsync();
        await _cacheService.InvalidateCategoryAsync(Category.Id);
        await _cacheService.InvalidateCategoryBySlugAsync(Category.Slug);
        _catalogSync.PublishUpsert(CatalogSyncEntityType.Category, Category.Id);

        TempData["SuccessMessage"] = "Kategori başarıyla güncellendi!";
        return RedirectToPage("./Index");
    }
}
