using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Services;
using BalonPark.Data;

namespace BalonPark.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class NotFoundModel : BasePage
{
    public int ErrorStatusCode { get; set; } = 404;

    public NotFoundModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
    }

    public IActionResult OnGet(int? statusCode = 404)
    {
        ErrorStatusCode = statusCode ?? 404;
        Response.StatusCode = ErrorStatusCode;
        return Page();
    }
}
