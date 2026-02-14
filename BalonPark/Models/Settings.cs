using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models;

/// <summary>
/// Settings tablosu için model sınıfı (migration 001 + 008, 009 ile uyumlu)
/// </summary>
public class Settings
{
    public const int UserNameMaxLength = 50;
    public const int PasswordMaxLength = 255;
    public const int CompanyNameMaxLength = 200;
    public const int LogoMaxLength = 500;
    public const int EmailMaxLength = 100;
    public const int PhoneMaxLength = 20;
    public const int FaxMaxLength = 20;
    public const int AddressMaxLength = 500;
    public const int CityMaxLength = 100;
    public const int DistrictMaxLength = 100;
    public const int PostalCodeMaxLength = 10;
    public const int CountryMaxLength = 100;
    public const int SocialUrlMaxLength = 255;
    public const int WorkingHoursMaxLength = 500;
    public const int MetaTitleMaxLength = 200;
    public const int MetaDescriptionMaxLength = 500;
    public const int MetaKeywordsMaxLength = 500;
    public const int GoogleTagMaxLength = 100;
    public const int GoogleTagManagerMaxLength = 50;
    public const int GoogleSiteVerificationMaxLength = 255;
    public const int GoogleShoppingMerchantIdMaxLength = 50;
    public const int GoogleShoppingServiceAccountEmailMaxLength = 255;

    public int Id { get; set; }

    [StringLength(UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;
    [StringLength(PasswordMaxLength)]
    public string? Password { get; set; }

    [StringLength(CompanyNameMaxLength)]
    public string CompanyName { get; set; } = string.Empty;
    public string? About { get; set; }
    [StringLength(LogoMaxLength)]
    public string? Logo { get; set; }

    [StringLength(EmailMaxLength)]
    public string Email { get; set; } = string.Empty;
    [StringLength(PhoneMaxLength)]
    public string? PhoneNumber { get; set; }
    [StringLength(PhoneMaxLength)]
    public string? PhoneNumber2 { get; set; }
    [StringLength(FaxMaxLength)]
    public string? Fax { get; set; }
    [StringLength(PhoneMaxLength)]
    public string? WhatsApp { get; set; }

    [StringLength(AddressMaxLength)]
    public string? Address { get; set; }
    [StringLength(CityMaxLength)]
    public string? City { get; set; }
    [StringLength(DistrictMaxLength)]
    public string? District { get; set; }
    [StringLength(PostalCodeMaxLength)]
    public string? PostalCode { get; set; }
    [StringLength(CountryMaxLength)]
    public string? Country { get; set; }

    [StringLength(SocialUrlMaxLength)]
    public string? Facebook { get; set; }
    [StringLength(SocialUrlMaxLength)]
    public string? Instagram { get; set; }
    [StringLength(SocialUrlMaxLength)]
    public string? Twitter { get; set; }
    [StringLength(SocialUrlMaxLength)]
    public string? LinkedIn { get; set; }
    [StringLength(SocialUrlMaxLength)]
    public string? YouTube { get; set; }

    [StringLength(WorkingHoursMaxLength)]
    public string? WorkingHours { get; set; }

    [StringLength(MetaTitleMaxLength)]
    public string? MetaTitle { get; set; }
    [StringLength(MetaDescriptionMaxLength)]
    public string? MetaDescription { get; set; }
    [StringLength(MetaKeywordsMaxLength)]
    public string? MetaKeywords { get; set; }

    [StringLength(GoogleTagMaxLength)]
    public string? GoogleTag { get; set; }
    [StringLength(GoogleTagManagerMaxLength)]
    public string? GoogleTagManager { get; set; }
    [StringLength(GoogleSiteVerificationMaxLength)]
    public string? GoogleSiteVerification { get; set; }

    [StringLength(GoogleShoppingMerchantIdMaxLength)]
    public string? GoogleShoppingMerchantId { get; set; }
    [StringLength(GoogleShoppingServiceAccountEmailMaxLength)]
    public string? GoogleShoppingServiceAccountEmail { get; set; }
    /// <summary>Google Cloud Service Account JSON key içeriği (admin panelden yönetilir).</summary>
    public string? GoogleShoppingServiceAccountKeyJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

