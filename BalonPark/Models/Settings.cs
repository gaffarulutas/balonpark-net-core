namespace BalonPark.Models;

/// <summary>
/// Settings tablosu için model sınıfı
/// </summary>
public class Settings
{
    public int Id { get; set; }
    
    // Admin Giriş Bilgileri
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    
    // Şirket Bilgileri
    public string CompanyName { get; set; } = string.Empty;
    public string? About { get; set; }
    public string? Logo { get; set; }
    
    // İletişim Bilgileri
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PhoneNumber2 { get; set; }
    public string? Fax { get; set; }
    public string? WhatsApp { get; set; }
    
    // Adres Bilgileri
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    
    // Sosyal Medya Hesapları
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? Twitter { get; set; }
    public string? LinkedIn { get; set; }
    public string? YouTube { get; set; }
    
    // Çalışma Saatleri
    public string? WorkingHours { get; set; }
    
    // SEO Bilgileri
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    
    // Google & Doğrulama
    /// <summary>Google Analytics / gtag.js Ölçüm ID (örn: G-XXXXXXXXXX)</summary>
    public string? GoogleTag { get; set; }
    /// <summary>Google Tag Manager Container ID (örn: GTM-XXXXXXX)</summary>
    public string? GoogleTagManager { get; set; }
    /// <summary>Google Site Verification meta content değeri</summary>
    public string? GoogleSiteVerification { get; set; }
    
    // Tarihler
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

