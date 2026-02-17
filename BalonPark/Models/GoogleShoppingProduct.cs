using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models
{
    public class GoogleShoppingProduct
    {
        /// <summary>Google Merchant Center custom_label alanları için maksimum karakter limiti.</summary>
        public const int CustomLabelMaxLength = 100;

        /// <summary>Google Merchant Center title alanı için maksimum karakter limiti.</summary>
        public const int TitleMaxLength = 150;

        /// <summary>Google Merchant Center description alanı için maksimum karakter limiti.</summary>
        public const int DescriptionMaxLength = 5000;

        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string OfferId { get; set; } = string.Empty;

        [Required]
        [MaxLength(TitleMaxLength)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(DescriptionMaxLength)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Link { get; set; } = string.Empty;

        [Required]
        public string ImageLink { get; set; } = string.Empty;

        [Required]
        public string Availability { get; set; } = "in stock";

        [Required]
        public string Condition { get; set; } = "new";

        [Required]
        public string Brand { get; set; } = "Balon Park";

        public string? Gtin { get; set; }

        public string? Mpn { get; set; }

        /// <summary>
        /// Ürünün benzersiz tanımlayıcılarının (GTIN, MPN, Marka) olup olmadığını belirtir.
        /// Üretici tarafından atanmış GTIN yoksa false olarak ayarlanmalıdır.
        /// </summary>
        public bool IdentifierExists { get; set; } = true;

        [Required]
        public decimal Price { get; set; }

        /// <summary>İndirimli fiyat (varsa).</summary>
        public decimal? SalePrice { get; set; }

        [Required]
        public string Currency { get; set; } = "TRY";

        [Required]
        public string ContentLanguage { get; set; } = "tr";

        [Required]
        public string TargetCountry { get; set; } = "TR";

        [Required]
        public string GoogleProductCategory { get; set; } = string.Empty;

        public string ProductType { get; set; } = string.Empty;

        public string? AdditionalImageLink { get; set; }

        public List<string> AdditionalImageLinks { get; set; } = new List<string>();

        public string? Color { get; set; }

        public string? Material { get; set; }

        public string? Size { get; set; }

        public string? AgeGroup { get; set; }

        public string? Gender { get; set; }

        public string? ItemGroupId { get; set; }

        public string? ShippingWeight { get; set; }

        public string? ShippingLength { get; set; }

        public string? ShippingWidth { get; set; }

        public string? ShippingHeight { get; set; }

        /// <summary>Ürünün öne çıkan özellikleri (2-100 arası, her biri maks 150 karakter).</summary>
        public List<string>? ProductHighlights { get; set; }

        [MaxLength(CustomLabelMaxLength)]
        public string? CustomLabel0 { get; set; }

        [MaxLength(CustomLabelMaxLength)]
        public string? CustomLabel1 { get; set; }

        [MaxLength(CustomLabelMaxLength)]
        public string? CustomLabel2 { get; set; }

        [MaxLength(CustomLabelMaxLength)]
        public string? CustomLabel3 { get; set; }

        [MaxLength(CustomLabelMaxLength)]
        public string? CustomLabel4 { get; set; }

        // Shipping (Kargo) Bilgileri
        public string? ShippingCountry { get; set; }

        public string? ShippingService { get; set; }

        public decimal? ShippingPrice { get; set; }
    }
}
