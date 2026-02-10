using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Pages
{
    public class HakkimizdaModel : BasePage
    {
        public HakkimizdaModel(CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
        {
        }

        public void OnGet()
        {
        }
    }
}
