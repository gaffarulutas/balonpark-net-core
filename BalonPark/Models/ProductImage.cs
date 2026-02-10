namespace BalonPark.Models;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string LargePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public bool IsMainImage { get; set; } = false;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

