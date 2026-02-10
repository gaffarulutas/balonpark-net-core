using Dapper;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Data;

public class CategoryRepository(DapperContext context, ICacheService cacheService)
{

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        // Önce cache'den kontrol et
        var cachedCategories = await cacheService.GetCategoriesAsync();
        if (cachedCategories != null)
        {
            return cachedCategories;
        }

        var query = @"
            SELECT c.*,
                   (SELECT TOP 1 pi.ThumbnailPath 
                    FROM Products p 
                    INNER JOIN ProductImages pi ON p.Id = pi.ProductId 
                    WHERE p.CategoryId = c.Id AND p.IsActive = 1
                    ORDER BY pi.IsMainImage DESC, p.Id, pi.DisplayOrder) as FirstProductImage
            FROM Categories c 
            WHERE c.IsActive = 1
            ORDER BY c.DisplayOrder, c.Name";
        using var connection = context.CreateConnection();
        var categories = await connection.QueryAsync<Category>(query);
        
        // Cache'e kaydet
        await cacheService.SetCategoriesAsync(categories);
        
        return categories;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        // Önce cache'den kontrol et
        var cachedCategory = await cacheService.GetCategoryByIdAsync(id);
        if (cachedCategory != null)
        {
            return cachedCategory;
        }

        var query = "SELECT * FROM Categories WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var category = await connection.QueryFirstOrDefaultAsync<Category>(query, new { Id = id });
        
        if (category != null)
        {
            // Cache'e kaydet
            await cacheService.SetCategoryAsync(category);
        }
        
        return category;
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        // Önce cache'den kontrol et
        var cachedCategory = await cacheService.GetCategoryBySlugAsync(slug);
        if (cachedCategory != null)
        {
            return cachedCategory;
        }

        var query = "SELECT * FROM Categories WHERE Slug = @Slug AND IsActive = 1";
        using var connection = context.CreateConnection();
        var category = await connection.QueryFirstOrDefaultAsync<Category>(query, new { Slug = slug });
        
        if (category != null)
        {
            // Cache'e kaydet
            await cacheService.SetCategoryAsync(category);
        }
        
        return category;
    }

    public async Task<int> CreateAsync(Category category)
    {
        var query = @"
            INSERT INTO Categories (Name, Slug, Description, IsActive, DisplayOrder, CreatedAt)
            VALUES (@Name, @Slug, @Description, @IsActive, @DisplayOrder, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        var newId = await connection.QuerySingleAsync<int>(query, category);
        
        // Cache'i temizle ve yeniden yükle
        await cacheService.InvalidateCategoriesAsync();
        
        // Tüm kategorileri yeniden yükle ve cache'e kaydet
        var allCategories = await GetAllCategoriesFromDatabaseAsync();
        await cacheService.SetCategoriesAsync(allCategories);
        
        return newId;
    }

    public async Task<int> UpdateAsync(Category category)
    {
        // Önce eski veriyi çek (eski slug için)
        var oldCategory = await GetByIdAsync(category.Id);
        var oldSlug = oldCategory?.Slug;
        
        var query = @"
            UPDATE Categories 
            SET Name = @Name,
                Slug = @Slug,
                Description = @Description, 
                IsActive = @IsActive,
                DisplayOrder = @DisplayOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, category);
        
        if (result > 0)
        {
            // Cache'i temizle
            await cacheService.InvalidateCategoriesAsync();
            await cacheService.InvalidateCategoryAsync(category.Id);
            await cacheService.InvalidateCategoryBySlugAsync(category.Slug);
            
            // Eski slug varsa onu da temizle
            if (!string.IsNullOrEmpty(oldSlug) && oldSlug != category.Slug)
            {
                await cacheService.InvalidateCategoryBySlugAsync(oldSlug);
            }
            
            // Tüm kategorileri yeniden yükle ve cache'e kaydet
            var allCategories = await GetAllCategoriesFromDatabaseAsync();
            await cacheService.SetCategoriesAsync(allCategories);
        }
        
        return result;
    }

    public async Task<int> DeleteAsync(int id)
    {
        var query = "DELETE FROM Categories WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id });
        
        if (result > 0)
        {
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateCategoriesAsync();
            await cacheService.InvalidateCategoryAsync(id);
            
            // Tüm kategorileri yeniden yükle ve cache'e kaydet
            var allCategories = await GetAllCategoriesFromDatabaseAsync();
            await cacheService.SetCategoriesAsync(allCategories);
        }
        
        return result;
    }

    public async Task<IEnumerable<Category>> SearchAsync(string query, int limit = 10)
    {
        var sql = @"
            SELECT TOP (@Limit) *
            FROM Categories
            WHERE IsActive = 1
            AND (Name LIKE @SearchQuery OR Description LIKE @SearchQuery)
            ORDER BY 
                CASE 
                    WHEN Name LIKE @ExactQuery THEN 1
                    WHEN Description LIKE @ExactQuery THEN 2
                    ELSE 3
                END,
                Name";
        
        var searchQuery = $"%{query}%";
        var exactQuery = $"{query}%";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Category>(sql, new { SearchQuery = searchQuery, ExactQuery = exactQuery, Limit = limit });
    }

    private async Task<IEnumerable<Category>> GetAllCategoriesFromDatabaseAsync()
    {
        var query = "SELECT * FROM Categories ORDER BY DisplayOrder, Name";
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Category>(query);
    }

    // Sıralama Metodları
    public async Task<bool> UpdateDisplayOrderAsync(int id, int newDisplayOrder)
    {
        var query = @"
            UPDATE Categories 
            SET DisplayOrder = @DisplayOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id, DisplayOrder = newDisplayOrder, UpdatedAt = DateTime.Now });
        
        if (result > 0)
        {
            await cacheService.InvalidateCategoriesAsync();
        }
        
        return result > 0;
    }

    public async Task<bool> ReorderCategoriesAsync(Dictionary<int, int> orderMap)
    {
        using var connection = context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            foreach (var (categoryId, displayOrder) in orderMap)
            {
                var query = "UPDATE Categories SET DisplayOrder = @DisplayOrder, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                await connection.ExecuteAsync(query, new { Id = categoryId, DisplayOrder = displayOrder, UpdatedAt = DateTime.Now }, transaction);
            }
            
            transaction.Commit();
            await cacheService.InvalidateCategoriesAsync();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }
}

