using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Services;
using BalonPark.Data;

namespace BalonPark.Pages;

public class PrivacyModel : BasePage
{
    private readonly ILogger<PrivacyModel> _logger;

    public PrivacyModel(ILogger<PrivacyModel> logger, CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        // BasePage'deki OnPageHandlerExecuting zaten UrlService ve SelectedCurrency'yi ViewData'ya ekliyor
    }
}