using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Helpers;

namespace BalonPark.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly SettingsRepository _settingsRepository;

    public LoginModel(SettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Settings'i cache'den yükle
        var settings = await _settingsRepository.GetFirstAsync();
        if (settings != null)
        {
            ViewData["SiteSettings"] = settings;
        }

        // Eğer zaten giriş yapmışsa admin paneline yönlendir
        if (HttpContext.Session.IsAdminLoggedIn())
        {
            return RedirectToPage("/Admin/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Settings'i cache'den yükle
        var settings = await _settingsRepository.GetFirstAsync();
        if (settings != null)
        {
            ViewData["SiteSettings"] = settings;
        }

        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kullanıcı adı ve şifre boş bırakılamaz.";
            return Page();
        }

        var admin = await _settingsRepository.ValidateAdminAsync(UserName, Password);

        if (admin == null)
        {
            ErrorMessage = "Kullanıcı adı veya şifre hatalı!";
            return Page();
        }

        // Session'a admin bilgilerini kaydet
        HttpContext.Session.SetAdminSession(admin.Id, admin.UserName, admin.Email);

        // Admin paneline yönlendir
        return RedirectToPage("/Admin/Index");
    }
}

