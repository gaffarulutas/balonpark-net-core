using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Services;
using BalonPark.Data;

namespace BalonPark.Pages;

public class IadePolitikasiModel : BasePage
{
    public IadePolitikasiModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
    }

    public void OnGet()
    {
    }
}
