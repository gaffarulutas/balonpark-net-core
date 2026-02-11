namespace BalonPark.Models;

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int SubCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TechnicalDescription { get; set; }
    /// <summary>Kısa özet (ürün kartı ve detay özet alanında gösterilir).</summary>
    public string? Summary { get; set; }
    public decimal Price { get; set; }
    public decimal UsdPrice { get; set; }
    public decimal EuroPrice { get; set; }
    public int Stock { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    // Ürün Teknik Bilgiler - Şişmiş ürün (Inflated product)
    public string? InflatedLength { get; set; }
    public string? InflatedWidth { get; set; }
    public string? InflatedHeight { get; set; }
    public int? UserCount { get; set; }

    // Montaj / demontaj (Assembly / disassembly) - süre saat (int), kişi sayısı (int)
    public int? AssemblyTime { get; set; }
    public int? RequiredPersonCount { get; set; }
    public string? FanDescription { get; set; }
    public decimal? FanWeightKg { get; set; }

    // Paketlenmiş ürünün özellikleri (Packaged product features)
    public string? PackagedLength { get; set; }
    public string? PackagedDepth { get; set; }
    public decimal? PackagedWeightKg { get; set; }
    public int? PackagePalletCount { get; set; }

    // Genel (General)
    public bool? HasCertificate { get; set; }
    public string? WarrantyDescription { get; set; }
    public string? AfterSalesService { get; set; }
    
    // Navigation properties
    public string? CategoryName { get; set; }
    public string? SubCategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public string? SubCategorySlug { get; set; }
    public string? MainImagePath { get; set; }
}

