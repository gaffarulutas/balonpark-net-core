namespace BalonPark.Models;

/// <summary>
/// "Sonuç bulunamadı" / empty state partial (_ResultNotFound) view modeli.
/// </summary>
public class ResultNotFoundViewModel
{
    /// <summary>Lucide ikon adı (örn: package-open, search, folder-open).</summary>
    public string Icon { get; set; } = "package-open";

    /// <summary>Başlık (örn: Ürün bulunmamaktadır).</summary>
    public string Title { get; set; } = "";

    /// <summary>Açıklama metni.</summary>
    public string Description { get; set; } = "";

    /// <summary>Birincil aksiyon buton metni (null ise buton gösterilmez).</summary>
    public string? PrimaryActionText { get; set; }

    /// <summary>Birincil aksiyon URL.</summary>
    public string? PrimaryActionUrl { get; set; }

    /// <summary>Birincil aksiyon Lucide ikon adı (örn: arrow-left, home).</summary>
    public string? PrimaryActionIcon { get; set; }

    /// <summary>Birincil buton primary (renkli) mi, yoksa outline mı.</summary>
    public bool PrimaryActionIsPrimary { get; set; } = false;

    /// <summary>İkincil aksiyon buton metni (null ise gösterilmez).</summary>
    public string? SecondaryActionText { get; set; }

    /// <summary>İkincil aksiyon URL.</summary>
    public string? SecondaryActionUrl { get; set; }

    /// <summary>İkincil aksiyon Lucide ikon adı.</summary>
    public string? SecondaryActionIcon { get; set; }
}
