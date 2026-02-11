using BalonPark.Models;

namespace BalonPark.Services;

public interface IAiService
{
    Task<ProductAiResponse> GenerateProductContentAsync(string productDescription);
    Task<BlogAiResponse> GenerateBlogContentAsync(string blogTopic);
}

public class ProductAiResponse
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TechnicalDescription { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public int SuggestedStock { get; set; }
}

public class BlogAiResponse
{
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string MetaKeywords { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}
