using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models;

public class Product
{
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 300;
    public const int SummaryMaxLength = 500;
    public const int DimensionsMaxLength = 100;
    public const int InflatedLengthMaxLength = 50;
    public const int FanDescriptionMaxLength = 200;
    public const int PackagedLengthMaxLength = 50;
    public const int WarrantyDescriptionMaxLength = 100;
    public const int AfterSalesServiceMaxLength = 500;
    public const int DeliveryDaysMaxLength = 100;
    public const int MaterialWeightMaxLength = 100;
    public const int ColorOptionsMaxLength = 200;

    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int SubCategoryId { get; set; }

    [StringLength(NameMaxLength, ErrorMessage = "Ürün adı en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(SlugMaxLength, ErrorMessage = "Slug en fazla {1} karakter olabilir.")]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? TechnicalDescription { get; set; }

    [StringLength(SummaryMaxLength, ErrorMessage = "Özet en fazla {1} karakter olabilir.")]
    public string? Summary { get; set; }
    public decimal Price { get; set; }
    public decimal UsdPrice { get; set; }
    public decimal EuroPrice { get; set; }
    public int Stock { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public int ViewCount { get; set; } = 0;

    // Ürün Teknik Bilgiler - Şişmiş ürün (Inflated product)
    [StringLength(InflatedLengthMaxLength)]
    public string? InflatedLength { get; set; }
    [StringLength(InflatedLengthMaxLength)]
    public string? InflatedWidth { get; set; }
    [StringLength(InflatedLengthMaxLength)]
    public string? InflatedHeight { get; set; }
    public int? UserCount { get; set; }

    // Montaj / demontaj (Assembly / disassembly) - süre saat (decimal, örn: 1,5), kişi sayısı (int)
    public decimal? AssemblyTime { get; set; }
    public int? RequiredPersonCount { get; set; }

    [StringLength(FanDescriptionMaxLength, ErrorMessage = "Fan açıklaması en fazla {1} karakter olabilir.")]
    public string? FanDescription { get; set; }
    public decimal? FanWeightKg { get; set; }

    // Paketlenmiş ürünün özellikleri (Packaged product features)
    [StringLength(PackagedLengthMaxLength)]
    public string? PackagedLength { get; set; }
    [StringLength(PackagedLengthMaxLength)]
    public string? PackagedDepth { get; set; }
    public decimal? PackagedWeightKg { get; set; }
    public int? PackagePalletCount { get; set; }

    // Genel (General)
    public bool HasCertificate { get; set; }

    [StringLength(WarrantyDescriptionMaxLength, ErrorMessage = "Garanti açıklaması en fazla {1} karakter olabilir.")]
    public string? WarrantyDescription { get; set; }

    [StringLength(AfterSalesServiceMaxLength, ErrorMessage = "Garanti sonrası hizmet en fazla {1} karakter olabilir.")]
    public string? AfterSalesService { get; set; }

    // Ürün etiketleri ve ek özellikler (detay sayfası badge/feature tags)
    public bool IsDiscounted { get; set; }
    public bool IsPopular { get; set; }
    public bool IsProjectSpecial { get; set; }
    /// <summary>Eski alan; detayda metin otomatik oluşturuluyorsa DeliveryDaysMin/Max kullan.</summary>
    [StringLength(DeliveryDaysMaxLength)]
    public string? DeliveryDays { get; set; }
    public int? DeliveryDaysMin { get; set; }
    public int? DeliveryDaysMax { get; set; }
    public bool IsFireResistant { get; set; }
    [StringLength(MaterialWeightMaxLength)]
    public string? MaterialWeight { get; set; }
    public decimal? MaterialWeightGrm2 { get; set; }
    [StringLength(ColorOptionsMaxLength)]
    public string? ColorOptions { get; set; }
    public decimal? InflatedWeightKg { get; set; }
    
    // Navigation properties
    public string? CategoryName { get; set; }
    public string? SubCategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public string? SubCategorySlug { get; set; }
    public string? MainImagePath { get; set; }
}

