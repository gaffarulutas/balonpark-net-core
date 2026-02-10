using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Services;
using BalonPark.Data;

namespace BalonPark.Pages;

public class SEOTestModel : BasePage
{
    public SEOTestModel(CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
    }

    public void OnGet()
    {
        // SEO test sayfası için özel bir işlem gerekmiyor
    }
}
