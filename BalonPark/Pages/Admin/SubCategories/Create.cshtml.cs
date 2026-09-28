using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Pages.Admin.SubCategories;

public class CreateModel : BaseAdminPage
{
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ICatalogSyncPublisher _catalogSync;

    public CreateModel(
        SubCategoryRepository subCategoryRepository,
        CategoryRepository categoryRepository,
        ICacheService cacheService,
        ICatalogSyncPublisher catalogSync)
    {
        _subCategoryRepository = subCategoryRepository;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _catalogSync = catalogSync;
    }

    [BindProperty]
    public SubCategory SubCategory { get; set; } = new();

    public new List<Category> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(SubCategory.Name))
        {
            ModelState.AddModelError("SubCategory.Name", "Alt kategori adı gereklidir.");
            Categories = (await _categoryRepository.GetAllAsync()).ToList();
            return Page();
        }

        if (SubCategory.CategoryId == 0)
        {
            ModelState.AddModelError("SubCategory.CategoryId", "Ana kategori seçilmelidir.");
            Categories = (await _categoryRepository.GetAllAsync()).ToList();
            return Page();
        }

        SubCategory.Slug = SlugHelper.GenerateSlug(SubCategory.Name);
        SubCategory.CreatedAt = DateTime.Now;
        var newId = await _subCategoryRepository.CreateAsync(SubCategory);
        SubCategory.Id = newId;

        await _cacheService.InvalidateSubCategoriesAsync();
        _catalogSync.PublishUpsert(CatalogSyncEntityType.SubCategory, newId);

        TempData["SuccessMessage"] = "Alt kategori başarıyla eklendi!";
        return RedirectToPage("./Index");
    }
}
