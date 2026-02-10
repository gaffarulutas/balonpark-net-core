using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.SubCategories;

public class EditModel : BaseAdminPage
{
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public EditModel(SubCategoryRepository subCategoryRepository, CategoryRepository categoryRepository, ICacheService cacheService)
    {
        _subCategoryRepository = subCategoryRepository;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
    }

    [BindProperty]
    public SubCategory SubCategory { get; set; } = new();

    public new List<Category> Categories { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var subCategory = await _subCategoryRepository.GetByIdAsync(id);
        
        if (subCategory == null)
        {
            return RedirectToPage("./Index");
        }

        SubCategory = subCategory;
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
        return Page();
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

        // Slug'ı otomatik güncelle
        SubCategory.Slug = SlugHelper.GenerateSlug(SubCategory.Name);
        SubCategory.UpdatedAt = DateTime.Now;
        await _subCategoryRepository.UpdateAsync(SubCategory);

        // Cache'i temizle
        await _cacheService.InvalidateSubCategoriesAsync();
        await _cacheService.InvalidateSubCategoryAsync(SubCategory.Id);
        await _cacheService.InvalidateSubCategoryBySlugAsync(SubCategory.Slug);

        TempData["SuccessMessage"] = "Alt kategori başarıyla güncellendi!";
        return RedirectToPage("./Index");
    }
}

