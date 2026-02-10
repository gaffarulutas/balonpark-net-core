using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.SubCategories;

public class CreateModel : BaseAdminPage
{
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public CreateModel(SubCategoryRepository subCategoryRepository, CategoryRepository categoryRepository, ICacheService cacheService)
    {
        _subCategoryRepository = subCategoryRepository;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
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

        // Slug'ı otomatik oluştur
        SubCategory.Slug = SlugHelper.GenerateSlug(SubCategory.Name);
        SubCategory.CreatedAt = DateTime.Now;
        await _subCategoryRepository.CreateAsync(SubCategory);

        // Cache'i temizle
        await _cacheService.InvalidateSubCategoriesAsync();

        TempData["SuccessMessage"] = "Alt kategori başarıyla eklendi!";
        return RedirectToPage("./Index");
    }
}

