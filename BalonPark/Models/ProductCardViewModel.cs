namespace BalonPark.Models;

/// <summary>
/// Ürün kartı (_ProductCard) partial view modeli.
/// </summary>
public class ProductCardViewModel
{
    /// <summary>Ürün ve ana görsel bilgisi.</summary>
    public ProductWithImage Item { get; set; } = null!;

    /// <summary>Ürün detay sayfası URL'i. Null ise /category/{CategorySlug}/{SubCategorySlug}/{Slug} kullanılır.</summary>
    public string? ProductUrl { get; set; }

    /// <summary>Gösterilecek para birimi (TL, USD, EUR, RUB).</summary>
    public string SelectedCurrency { get; set; } = "TL";
}
