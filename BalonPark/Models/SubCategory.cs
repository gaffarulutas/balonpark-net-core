namespace BalonPark.Models;

public class SubCategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
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

