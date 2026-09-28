using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Pages.Admin.Categories;

public class CreateModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ICatalogSyncPublisher _catalogSync;

    public CreateModel(CategoryRepository categoryRepository, ICacheService cacheService, ICatalogSyncPublisher catalogSync)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _catalogSync = catalogSync;
    }

    [BindProperty]
    public Category Category { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Category.Name))
        {
            ModelState.AddModelError("Category.Name", "Kategori adı gereklidir.");
            return Page();
        }

        Category.Slug = SlugHelper.GenerateSlug(Category.Name);
        Category.CreatedAt = DateTime.Now;
        var newId = await _categoryRepository.CreateAsync(Category);
        Category.Id = newId;

        await _cacheService.InvalidateCategoriesAsync();
        _catalogSync.PublishUpsert(CatalogSyncEntityType.Category, newId);

        TempData["SuccessMessage"] = "Kategori başarıyla eklendi!";
        return RedirectToPage("./Index");
    }
}
