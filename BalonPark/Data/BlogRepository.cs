using Dapper;
using BalonPark.Models;
using BalonPark.Services;
using BalonPark.Helpers;

namespace BalonPark.Data;

public class BlogRepository(DapperContext context, ICacheService cacheService)
{

    public async Task<IEnumerable<Blog>> GetAllAsync()
    {
        try
        {
            // Önce cache'den kontrol et
            var cachedBlogs = await cacheService.GetBlogsAsync();
            if (cachedBlogs != null)
            {
                return cachedBlogs;
            }

            var query = @"
                SELECT * FROM Blogs 
                WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
                ORDER BY IsFeatured DESC, CreatedAt DESC";
            
            using var connection = context.CreateConnection();
            var blogs = await connection.QueryAsync<Blog>(query);
            
            // Cache'e kaydet
            await cacheService.SetBlogsAsync(blogs);
            
            return blogs;
        }
        catch (Exception)
        {
            // If table doesn't exist or other database error, return empty list
            return new List<Blog>();
        }
    }

    public async Task<IEnumerable<Blog>> GetAllForAdminAsync()
    {
        try
        {
            var query = @"
                SELECT * FROM Blogs 
                ORDER BY IsFeatured DESC, CreatedAt DESC";
            
            using var connection = context.CreateConnection();
            var blogs = await connection.QueryAsync<Blog>(query);
            
            return blogs;
        }
        catch (Exception)
        {
            // If table doesn't exist or other database error, return empty list
            return new List<Blog>();
        }
    }

    public async Task<IEnumerable<Blog>> GetFeaturedAsync(int limit = 5)
    {
        try
        {
            var query = @"
                SELECT TOP (@Limit) * FROM Blogs 
                WHERE IsActive = 1 AND IsFeatured = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
                ORDER BY CreatedAt DESC";
            
            using var connection = context.CreateConnection();
            return await connection.QueryAsync<Blog>(query, new { Limit = limit });
        }
        catch (Exception)
        {
            return new List<Blog>();
        }
    }

    public async Task<IEnumerable<Blog>> GetLatestAsync(int limit = 10)
    {
        try
        {
            var query = @"
                SELECT TOP (@Limit) * FROM Blogs 
                WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
                ORDER BY CreatedAt DESC";
            
            using var connection = context.CreateConnection();
            return await connection.QueryAsync<Blog>(query, new { Limit = limit });
        }
        catch (Exception)
        {
            return new List<Blog>();
        }
    }

    public async Task<Blog?> GetByIdAsync(int id)
    {
        // Önce cache'den kontrol et
        var cachedBlog = await cacheService.GetBlogByIdAsync(id);
        if (cachedBlog != null)
        {
            return cachedBlog;
        }

        var query = @"
            SELECT * FROM Blogs 
            WHERE Id = @Id AND IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())";
        
        using var connection = context.CreateConnection();
        var blog = await connection.QueryFirstOrDefaultAsync<Blog>(query, new { Id = id });
        
        if (blog != null)
        {
            // Cache'e kaydet
            await cacheService.SetBlogAsync(blog);
        }
        
        return blog;
    }

    public async Task<Blog?> GetByIdForAdminAsync(int id)
    {
        var query = "SELECT * FROM Blogs WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Blog>(query, new { Id = id });
    }

    public async Task<Blog?> GetBySlugAsync(string slug)
    {
        // Önce cache'den kontrol et
        var cachedBlog = await cacheService.GetBlogBySlugAsync(slug);
        if (cachedBlog != null)
        {
            return cachedBlog;
        }

        var query = @"
            SELECT * FROM Blogs 
            WHERE Slug = @Slug AND IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())";
        
        using var connection = context.CreateConnection();
        var blog = await connection.QueryFirstOrDefaultAsync<Blog>(query, new { Slug = slug });
        
        if (blog != null)
        {
            // Cache'e kaydet
            await cacheService.SetBlogAsync(blog);
        }
        
        return blog;
    }

    public async Task<IEnumerable<Blog>> SearchAsync(string query, int limit = 10)
    {
        var sql = @"
            SELECT TOP (@Limit) *
            FROM Blogs
            WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
            AND (Title LIKE @SearchQuery 
                 OR Content LIKE @SearchQuery 
                 OR Excerpt LIKE @SearchQuery
                 OR MetaKeywords LIKE @SearchQuery)
            ORDER BY 
                CASE 
                    WHEN Title LIKE @ExactQuery THEN 1
                    WHEN Excerpt LIKE @ExactQuery THEN 2
                    WHEN Content LIKE @ExactQuery THEN 3
                    WHEN MetaKeywords LIKE @ExactQuery THEN 4
                    ELSE 5
                END,
                CreatedAt DESC";
        
        var searchQuery = $"%{query}%";
        var exactQuery = $"{query}%";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Blog>(sql, new { SearchQuery = searchQuery, ExactQuery = exactQuery, Limit = limit });
    }

    public async Task<IEnumerable<Blog>> GetRelatedBlogsAsync(int blogId, int limit = 5)
    {
        var query = @"
            SELECT TOP (@Limit) b2.*
            FROM Blogs b1
            CROSS JOIN Blogs b2
            WHERE b1.Id = @BlogId 
            AND b2.Id != @BlogId
            AND b2.IsActive = 1 
            AND (b2.PublishedAt IS NULL OR b2.PublishedAt <= GETDATE())
            ORDER BY 
                CASE 
                    WHEN b2.Category = b1.Category THEN 1
                    ELSE 2
                END,
                b2.CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Blog>(query, new { BlogId = blogId, Limit = limit });
    }

    public async Task<int> CreateAsync(Blog blog)
    {
        try
        {
            var query = @"
                INSERT INTO Blogs (Title, Slug, Content, Excerpt, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, 
                                  IsActive, IsFeatured, ViewCount, CreatedAt, PublishedAt, AuthorName, Category)
                VALUES (@Title, @Slug, @Content, @Excerpt, @FeaturedImage, @MetaTitle, @MetaDescription, @MetaKeywords,
                        @IsActive, @IsFeatured, @ViewCount, @CreatedAt, @PublishedAt, @AuthorName, @Category);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            using var connection = context.CreateConnection();
            var newId = await connection.QuerySingleAsync<int>(query, blog);
            
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateBlogsAsync();
            
            // Tüm blogları yeniden yükle ve cache'e kaydet
            var allBlogs = await GetAllBlogsFromDatabaseAsync();
            await cacheService.SetBlogsAsync(allBlogs);
            
            return newId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Blog oluşturulurken veritabanı hatası: {ex.Message}", ex);
        }
    }

    public async Task<int> UpdateAsync(Blog blog)
    {
        // Önce eski veriyi çek (eski slug için)
        var oldBlog = await GetByIdAsync(blog.Id);
        var oldSlug = oldBlog?.Slug;
        
        var query = @"
            UPDATE Blogs 
            SET Title = @Title,
                Slug = @Slug,
                Content = @Content,
                Excerpt = @Excerpt,
                FeaturedImage = @FeaturedImage,
                MetaTitle = @MetaTitle,
                MetaDescription = @MetaDescription,
                MetaKeywords = @MetaKeywords,
                IsActive = @IsActive,
                IsFeatured = @IsFeatured,
                ViewCount = @ViewCount,
                UpdatedAt = @UpdatedAt,
                PublishedAt = @PublishedAt,
                AuthorName = @AuthorName,
                Category = @Category
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, blog);
        
        if (result > 0)
        {
            // Cache'i temizle
            await cacheService.InvalidateBlogsAsync();
            await cacheService.InvalidateBlogAsync(blog.Id);
            await cacheService.InvalidateBlogBySlugAsync(blog.Slug);
            
            // Eski slug varsa onu da temizle
            if (!string.IsNullOrEmpty(oldSlug) && oldSlug != blog.Slug)
            {
                await cacheService.InvalidateBlogBySlugAsync(oldSlug);
            }
            
            // Tüm blogları yeniden yükle ve cache'e kaydet
            var allBlogs = await GetAllBlogsFromDatabaseAsync();
            await cacheService.SetBlogsAsync(allBlogs);
        }
        
        return result;
    }

    public async Task<int> DeleteAsync(int id)
    {
        // Önce blog'u çek (resim yolunu almak için) - Admin için tüm blogları getir
        var blog = await GetByIdForAdminAsync(id);
        if (blog == null)
        {
            return 0;
        }

        var query = "DELETE FROM Blogs WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id });
        
        if (result > 0)
        {
            // Blog resmini sil
            if (!string.IsNullOrEmpty(blog.FeaturedImage))
            {
                ImageHelper.DeleteBlogImage(blog.FeaturedImage);
            }
            
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateBlogsAsync();
            await cacheService.InvalidateBlogAsync(id);
            
            // Tüm blogları yeniden yükle ve cache'e kaydet
            var allBlogs = await GetAllBlogsFromDatabaseAsync();
            await cacheService.SetBlogsAsync(allBlogs);
        }
        
        return result;
    }

    public async Task<int> IncrementViewCountAsync(int id)
    {
        var query = "UPDATE Blogs SET ViewCount = ViewCount + 1 WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id });
        
        if (result > 0)
        {
            // Cache'i temizle
            await cacheService.InvalidateBlogAsync(id);
        }
        
        return result;
    }

    public async Task<IEnumerable<Blog>> GetByCategoryAsync(string category, int limit = 10)
    {
        var query = @"
            SELECT TOP (@Limit) * FROM Blogs 
            WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
            AND Category = @Category
            ORDER BY CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Blog>(query, new { Category = category, Limit = limit });
    }

    public async Task<IEnumerable<Blog>> GetByTagAsync(string tag, int limit = 10)
    {
        var query = @"
            SELECT TOP (@Limit) * FROM Blogs 
            WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
            AND (MetaKeywords LIKE @TagSearch)
            ORDER BY CreatedAt DESC";
        
        var tagSearch = $"%{tag}%";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Blog>(query, new { TagSearch = tagSearch, Limit = limit });
    }

    private async Task<IEnumerable<Blog>> GetAllBlogsFromDatabaseAsync()
    {
        var query = @"
            SELECT * FROM Blogs 
            WHERE IsActive = 1 AND (PublishedAt IS NULL OR PublishedAt <= GETDATE())
            ORDER BY CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Blog>(query);
    }
}
