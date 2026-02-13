using System.ComponentModel.DataAnnotations;

namespace BalonPark.Models;

public class SubCategory
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 200;
    public const int DescriptionMaxLength = 500;

    public int Id { get; set; }
    public int CategoryId { get; set; }

    [StringLength(NameMaxLength, ErrorMessage = "Alt kategori adı en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(SlugMaxLength, ErrorMessage = "Slug en fazla {1} karakter olabilir.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(DescriptionMaxLength, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    
    // First product image for card display
    public string? FirstProductImage { get; set; }
    
    // Product count for display
    public int ProductCount { get; set; }
}

