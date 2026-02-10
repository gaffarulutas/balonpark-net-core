using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Pages
{
    public class KullanimKosullariModel : BasePage
    {
        public KullanimKosullariModel(CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
        {
        }

        public void OnGet()
        {
        }
    }
}
