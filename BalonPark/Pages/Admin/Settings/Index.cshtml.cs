using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Settings;

public class IndexModel : BaseAdminPage
{
    private readonly SettingsRepository _settingsRepository;
    private readonly ICacheService _cacheService;

    public IndexModel(SettingsRepository settingsRepository, ICacheService cacheService)
    {
        _settingsRepository = settingsRepository;
        _cacheService = cacheService;
    }

    [BindProperty]
    public Models.Settings Settings { get; set; } = new();
    
    [BindProperty]
    public IFormFile? LogoFile { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var settings = await _settingsRepository.GetFirstAsync();
        if (settings == null)
        {
            // Eğer hiç ayar yoksa varsayılan değerlerle bir tane oluştur ve kaydet
            Settings = new Models.Settings
            {
                UserName = "admin",
                Password = "1",
                CompanyName = "Site Adı",
                Email = "info@site.com",
                Country = "Türkiye",
                CreatedAt = DateTime.Now
            };
            
            await _settingsRepository.CreateAsync(Settings);
        }
        else
        {
            Settings = settings;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Lütfen gerekli alanları doldurun!";
            return Page();
        }

        try
        {
            // Mevcut ayarları getir
            var existingSettings = await _settingsRepository.GetFirstAsync();
            if (existingSettings == null)
            {
                ErrorMessage = "Ayarlar bulunamadı!";
                return Page();
            }
            
            // Logo dosyası yüklenmişse işle
            if (LogoFile != null && LogoFile.Length > 0)
            {
                // Eski logoyu sil (eğer varsa ve uploads/logo klasöründeyse)
                if (!string.IsNullOrEmpty(existingSettings.Logo))
                {
                    ImageHelper.DeleteLogo(existingSettings.Logo);
                }
                
                // Yeni logoyu kaydet
                Settings.Logo = await ImageHelper.SaveLogoAsync(LogoFile);
            }
            else
            {
                // Logo değişmemişse mevcut logoyu koru
                Settings.Logo = existingSettings.Logo;
            }
            
            // Şifre boşsa mevcut şifreyi koru
            if (string.IsNullOrWhiteSpace(Settings.Password))
            {
                Settings.Password = existingSettings.Password;
            }
            
            Settings.UpdatedAt = DateTime.Now;
            
            // Her zaman güncelleme yap (Settings tablosu tek satırlıktır)
            var result = await _settingsRepository.UpdateAsync(Settings);
            if (result)
            {
                // Tüm cache'leri temizle çünkü Settings her yerde kullanılıyor
                await _cacheService.InvalidateAllAsync();
                
                SuccessMessage = "Ayarlar başarıyla güncellendi!";
            }
            else
            {
                ErrorMessage = "Ayarlar güncellenirken hata oluştu!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Bir hata oluştu: {ex.Message}";
        }

        return RedirectToPage();
    }
}

