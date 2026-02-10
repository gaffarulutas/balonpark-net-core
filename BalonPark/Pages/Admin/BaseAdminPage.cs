using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using BalonPark.Helpers;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin;

/// <summary>
/// Admin sayfaları için base page model - Session kontrolü yapar ve Settings'i cache'den yükler
/// </summary>
public abstract class BaseAdminPage : PageModel
{
    private static SettingsRepository? _settingsRepository;
    
    public int AdminId { get; private set; }
    public string AdminUserName { get; private set; } = string.Empty;
    public string AdminEmail { get; private set; } = string.Empty;
    
    // Layout için gerekli properties
    public List<Category> Categories { get; set; } = new();
    public IUrlService? UrlService { get; set; }
    public ICurrencyCookieService? CurrencyCookieService { get; set; }
    public Models.Settings? SiteSettings { get; private set; }

    // Dependency injection için constructor
    protected BaseAdminPage()
    {
    }
    
    // Static method ile SettingsRepository'yi kaydet (Program.cs'den çağrılacak)
    public static void SetSettingsRepository(SettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        // Session kontrolü
        if (!HttpContext.Session.IsAdminLoggedIn())
        {
            context.Result = RedirectToPage("/Admin/Login");
            return;
        }

        // Admin bilgilerini al
        AdminId = HttpContext.Session.GetAdminId() ?? 0;
        AdminUserName = HttpContext.Session.GetAdminUserName() ?? string.Empty;
        AdminEmail = HttpContext.Session.GetAdminEmail() ?? string.Empty;

        // Settings'i cache'den yükle (eğer repository set edilmişse)
        if (_settingsRepository != null)
        {
            try
            {
                var settings = _settingsRepository.GetFirstAsync().GetAwaiter().GetResult();
                if (settings != null)
                {
                    SiteSettings = settings;
                    ViewData["SiteSettings"] = SiteSettings;
                    ViewData["AdminUserName"] = AdminUserName;
                    ViewData["AdminEmail"] = AdminEmail;
                }
            }
            catch (Exception)
            {
                // Settings yüklenemezse devam et
            }
        }

        base.OnPageHandlerExecuting(context);
    }
}

