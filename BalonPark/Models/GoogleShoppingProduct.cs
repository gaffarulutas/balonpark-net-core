using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models
{
    public class GoogleShoppingProduct
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string OfferId { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
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

        [Required]
        public string Gtin { get; set; } = string.Empty;

        [Required]
        public string Mpn { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Currency { get; set; } = "TRY";

        [Required]
        public string ContentLanguage { get; set; } = "tr";

        [Required]
        public string TargetCountry { get; set; } = "TR";

        [Required]
        public string GoogleProductCategory { get; set; } = string.Empty;

        [Required]
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

        public string? CustomLabel0 { get; set; }

        public string? CustomLabel1 { get; set; }

        public string? CustomLabel2 { get; set; }

        public string? CustomLabel3 { get; set; }

        public string? CustomLabel4 { get; set; }

        // Shipping (Kargo) Bilgileri
        public string? ShippingCountry { get; set; }
        
        public string? ShippingService { get; set; }
        
        public decimal? ShippingPrice { get; set; }
    }
}
