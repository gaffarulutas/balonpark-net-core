using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Categories;

public class CreateModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public CreateModel(CategoryRepository categoryRepository, ICacheService cacheService)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
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

        // Slug'ı otomatik oluştur
        Category.Slug = SlugHelper.GenerateSlug(Category.Name);
        Category.CreatedAt = DateTime.Now;
        await _categoryRepository.CreateAsync(Category);

        // Cache'i temizle
        await _cacheService.InvalidateCategoriesAsync();

        TempData["SuccessMessage"] = "Kategori başarıyla eklendi!";
        return RedirectToPage("./Index");
    }
}

