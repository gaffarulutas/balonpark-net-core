using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

/// <summary>
/// Public sayfalar için base page model - Kategorileri ve Settings'i otomatik yükler
/// </summary>
public abstract class BasePage : PageModel
{
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly SettingsRepository _settingsRepository;
    protected readonly IUrlService _urlService;
    protected readonly ICurrencyCookieService _currencyCookieService;

    public List<Category> Categories { get; private set; } = new();
    public string SelectedCurrency { get; private set; } = "TL";
    public Models.Settings SiteSettings { get; private set; } = new();

    protected BasePage(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService, 
        ICurrencyCookieService currencyCookieService)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _settingsRepository = settingsRepository;
        _urlService = urlService;
        _currencyCookieService = currencyCookieService;
    }

    public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
    {
        // Kategorileri yükle - bu method sync olduğu için GetAwaiter().GetResult() kullanıyoruz
        var categories = _categoryRepository.GetAllAsync().GetAwaiter().GetResult();
        Categories = categories.Where(c => c.IsActive).ToList();
        
        // Her kategori için alt kategorileri yükle
        foreach (var category in Categories)
        {
            try
            {
                var subCategories = _subCategoryRepository.GetByCategoryIdAsync(category.Id).GetAwaiter().GetResult();
                category.SubCategories = subCategories?.Where(sc => sc.IsActive).ToList() ?? new List<SubCategory>();
            }
            catch
            {
                // Eğer alt kategoriler yüklenemezse boş liste ata
                category.SubCategories = new List<SubCategory>();
            }
        }

        // Settings'i yükle
        var settings = _settingsRepository.GetFirstAsync().GetAwaiter().GetResult();
        if (settings != null)
        {
            SiteSettings = settings;
        }

        // Currency'yi cookie'den al
        SelectedCurrency = _currencyCookieService.GetSelectedCurrency();

        // UrlService, SelectedCurrency, SiteSettings ve Categories'i ViewData'ya ekle (Layout / header mega menü için)
        ViewData["UrlService"] = _urlService;
        ViewData["SelectedCurrency"] = SelectedCurrency;
        ViewData["SiteSettings"] = SiteSettings;
        ViewData["Categories"] = Categories;

        base.OnPageHandlerExecuting(context);
    }
}
