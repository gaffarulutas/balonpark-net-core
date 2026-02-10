using BalonPark.Models;

namespace BalonPark.Services;

public interface IBlogService
{
    Task<IEnumerable<Blog>> GetAllBlogsAsync();
    Task<IEnumerable<Blog>> GetFeaturedBlogsAsync(int limit = 5);
    Task<IEnumerable<Blog>> GetLatestBlogsAsync(int limit = 10);
    Task<Blog?> GetBlogBySlugAsync(string slug);
    Task<IEnumerable<Blog>> SearchBlogsAsync(string query, int limit = 10);
    Task<IEnumerable<Blog>> GetRelatedBlogsAsync(int blogId, int limit = 5);
    Task<BlogWithProducts> GetBlogWithProductsAsync(string slug);
    Task<int> IncrementViewCountAsync(int blogId);
    Task<string> GenerateSlugAsync(string title);
    Task<string> GenerateMetaDescriptionAsync(string content, int maxLength = 160);
    Task<string[]> ExtractKeywordsAsync(string content);
    Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(string category, int limit = 10);
    Task<IEnumerable<Blog>> GetBlogsByTagAsync(string tag, int limit = 10);
}
