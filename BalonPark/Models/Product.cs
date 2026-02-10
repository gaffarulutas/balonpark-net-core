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
    public string? Dimensions { get; set; }
    public decimal Price { get; set; }
    public decimal UsdPrice { get; set; }
    public decimal EuroPrice { get; set; }
    public int Stock { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public string? CategoryName { get; set; }
    public string? SubCategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public string? SubCategorySlug { get; set; }
    public string? MainImagePath { get; set; }
}

