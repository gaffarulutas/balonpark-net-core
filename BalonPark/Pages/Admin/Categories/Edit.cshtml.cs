using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Categories;

public class EditModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public EditModel(CategoryRepository categoryRepository, ICacheService cacheService)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
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

        // Slug'ı otomatik güncelle
        Category.Slug = SlugHelper.GenerateSlug(Category.Name);
        Category.UpdatedAt = DateTime.Now;
        await _categoryRepository.UpdateAsync(Category);

        // Cache'i temizle
        await _cacheService.InvalidateCategoriesAsync();
        await _cacheService.InvalidateCategoryAsync(Category.Id);
        await _cacheService.InvalidateCategoryBySlugAsync(Category.Slug);

        TempData["SuccessMessage"] = "Kategori başarıyla güncellendi!";
        return RedirectToPage("./Index");
    }
}

