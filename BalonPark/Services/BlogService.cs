using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BalonPark.Services;

public class BlogService(
    BlogRepository blogRepository,
    ProductRepository productRepository,
    ProductImageRepository productImageRepository,
    ICacheService cacheService,
    ILogger<BlogService> logger) : IBlogService
{

    public async Task<IEnumerable<Blog>> GetAllBlogsAsync()
    {
        return await blogRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Blog>> GetFeaturedBlogsAsync(int limit = 5)
    {
        var cachedBlogs = await cacheService.GetFeaturedBlogsAsync();
        if (cachedBlogs != null)
            return cachedBlogs.Take(limit);

        var blogs = await blogRepository.GetFeaturedAsync(limit);
        await cacheService.SetFeaturedBlogsAsync(blogs);
        return blogs;
    }

    public async Task<IEnumerable<Blog>> GetLatestBlogsAsync(int limit = 10)
    {
        var cachedBlogs = await cacheService.GetLatestBlogsAsync();
        if (cachedBlogs != null)
            return cachedBlogs.Take(limit);

        var blogs = await blogRepository.GetLatestAsync(limit);
        await cacheService.SetLatestBlogsAsync(blogs);
        return blogs;
    }

    public async Task<Blog?> GetBlogBySlugAsync(string slug)
    {
        try
        {
            return await blogRepository.GetBySlugAsync(slug);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blog getirme hatası. Slug: {Slug}", slug);
            return null;
        }
    }

    public async Task<IEnumerable<Blog>> SearchBlogsAsync(string query, int limit = 10)
    {
        var cachedBlogs = await cacheService.SearchBlogsAsync(query);
        if (cachedBlogs != null)
            return cachedBlogs.Take(limit);

        var blogs = await blogRepository.SearchAsync(query, limit);
        await cacheService.SetSearchBlogsAsync(query, blogs);
        return blogs;
    }

    public async Task<IEnumerable<Blog>> GetRelatedBlogsAsync(int blogId, int limit = 5)
    {
        return await blogRepository.GetRelatedBlogsAsync(blogId, limit);
    }

    public async Task<BlogWithProducts> GetBlogWithProductsAsync(string slug)
    {
        var blog = await GetBlogBySlugAsync(slug);
        if (blog == null)
        {
            throw new ArgumentException("Blog not found");
        }

        var blogWithProducts = new BlogWithProducts
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Content = blog.Content,
            Excerpt = blog.Excerpt,
            FeaturedImage = blog.FeaturedImage,
            MetaTitle = blog.MetaTitle,
            MetaDescription = blog.MetaDescription,
            MetaKeywords = blog.MetaKeywords,
            IsActive = blog.IsActive,
            IsFeatured = blog.IsFeatured,
            ViewCount = blog.ViewCount,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            PublishedAt = blog.PublishedAt,
            AuthorName = blog.AuthorName,
            Tags = blog.Tags,
            Category = blog.Category,
            RelatedProducts = new List<ProductWithImage>()
        };

        // Blog ile ilgili ürünleri getir
        if (!string.IsNullOrEmpty(blog.Category))
        {
            var relatedProducts = await GetRelatedProductsByCategoryAsync(blog.Category);
            blogWithProducts.RelatedProducts = relatedProducts.Take(6).ToList();
        }

        return blogWithProducts;
    }

    public async Task<int> IncrementViewCountAsync(int blogId)
    {
        return await blogRepository.IncrementViewCountAsync(blogId);
    }

    public async Task<string> GenerateSlugAsync(string title)
    {
        return await Task.FromResult(SlugHelper.GenerateSlug(title));
    }

    public async Task<string> GenerateMetaDescriptionAsync(string content, int maxLength = 160)
    {
        // HTML etiketlerini temizle
        var cleanContent = Regex.Replace(content, "<.*?>", " ");
        
        // Fazla boşlukları temizle
        cleanContent = Regex.Replace(cleanContent, @"\s+", " ").Trim();
        
        // Maksimum uzunluğa göre kes
        if (cleanContent.Length <= maxLength)
        {
            return await Task.FromResult(cleanContent);
        }
        
        // Son kelimeyi tamamlamaya çalış
        var truncated = cleanContent.Substring(0, maxLength);
        var lastSpaceIndex = truncated.LastIndexOf(' ');
        
        if (lastSpaceIndex > maxLength * 0.8) // %80'den fazlası varsa kelimeyi tamamla
        {
            truncated = truncated.Substring(0, lastSpaceIndex);
        }
        
        return await Task.FromResult(truncated + "...");
    }

    public async Task<string[]> ExtractKeywordsAsync(string content)
    {
        // HTML etiketlerini temizle
        var cleanContent = Regex.Replace(content, "<.*?>", " ");
        
        // Türkçe stop words
        var stopWords = new HashSet<string>
        {
            "ve", "ile", "için", "olan", "bu", "şu", "bir", "da", "de", "den", "dan",
            "çok", "daha", "en", "çok", "az", "var", "yok", "olan", "olanlar",
            "gibi", "kadar", "sonra", "önce", "içinde", "dışında", "üzerinde",
            "altında", "yanında", "karşısında", "arasında", "için", "göre",
            "rağmen", "dolayı", "sayesinde", "nedeniyle", "sonucunda",
            "hakkında", "ile ilgili", "konusunda", "dair", "hakkında",
            "nasıl", "neden", "niçin", "ne", "kim", "kime", "kimden", "kimin",
            "nerede", "nereye", "nereden", "ne zaman", "kaç", "hangi"
        };
        
        // Kelimeleri ayır ve temizle
        var words = Regex.Matches(cleanContent, @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(10)
            .ToArray();
        
        return await Task.FromResult(words);
    }

    public async Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(string category, int limit = 10)
    {
        return await blogRepository.GetByCategoryAsync(category, limit);
    }

    public async Task<IEnumerable<Blog>> GetBlogsByTagAsync(string tag, int limit = 10)
    {
        return await blogRepository.GetByTagAsync(tag, limit);
    }

    private async Task<IEnumerable<ProductWithImage>> GetRelatedProductsByCategoryAsync(string category)
    {
        var products = await productRepository.SearchAsync(category, 10);
        var productsWithImages = new List<ProductWithImage>();

        foreach (var product in products)
        {
            var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
            var productWithImage = new ProductWithImage
            {
                Product = product,
                MainImage = mainImage
            };
            productsWithImages.Add(productWithImage);
        }

        return productsWithImages;
    }
}
