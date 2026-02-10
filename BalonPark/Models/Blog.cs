namespace BalonPark.Models;

public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string? FeaturedImage { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // Navigation properties
    public string? AuthorName { get; set; } = "Balon Park";
    public List<string>? Tags { get; set; }
    public string? Category { get; set; }
}

public class BlogWithProducts : Blog
{
    public List<ProductWithImage> RelatedProducts { get; set; } = new();
}
