namespace BalonPark.Models;

/// <summary>
/// Breadcrumb navigasyon öğesi. Url null ise mevcut sayfa (link değil) olarak gösterilir.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>Görüntülenecek metin.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Link URL. Null ise mevcut sayfa (tıklanamaz).</summary>
    public string? Url { get; set; }

    /// <summary>Link breadcrumb öğesi oluşturur.</summary>
    public static BreadcrumbItem Link(string label, string url) => new() { Label = label, Url = url };

    /// <summary>Mevcut sayfa (aktif) breadcrumb öğesi oluşturur.</summary>
    public static BreadcrumbItem Current(string label) => new() { Label = label, Url = null };
}
