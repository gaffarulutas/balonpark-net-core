using Dapper;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Data;

public class SubCategoryRepository(DapperContext context, ICacheService cacheService)
{

    public async Task<IEnumerable<SubCategory>> GetAllAsync()
    {
        // Önce cache'den kontrol et
        var cachedSubCategories = await cacheService.GetSubCategoriesAsync();
        if (cachedSubCategories != null)
        {
            return cachedSubCategories;
        }

        var query = @"
            SELECT sc.*, c.Name as CategoryName 
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            ORDER BY c.DisplayOrder, c.Name, sc.DisplayOrder, sc.Name";
        
        using var connection = context.CreateConnection();
        var subCategories = await connection.QueryAsync<SubCategory>(query);
        
        // Cache'e kaydet
        await cacheService.SetSubCategoriesAsync(subCategories);
        
        return subCategories;
    }

    public async Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId)
    {
        // Önce cache'den kontrol et
        var cachedSubCategories = await cacheService.GetSubCategoriesByCategoryIdAsync(categoryId);
        if (cachedSubCategories != null)
        {
            return cachedSubCategories;
        }

        var query = @"
            SELECT sc.*, c.Name as CategoryName 
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            WHERE sc.CategoryId = @CategoryId AND sc.IsActive = 1
            ORDER BY sc.DisplayOrder, sc.Name";
        
        using var connection = context.CreateConnection();
        var subCategories = await connection.QueryAsync<SubCategory>(query, new { CategoryId = categoryId });
        
        return subCategories;
    }

    public async Task<IEnumerable<SubCategory>> GetByCategorySlugAsync(string categorySlug)
    {
        // Önce cache'den kontrol et
        var cachedSubCategories = await cacheService.GetSubCategoriesByCategorySlugAsync(categorySlug);
        if (cachedSubCategories != null)
        {
            return cachedSubCategories;
        }

        var query = @"
            SELECT sc.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   (SELECT TOP 1 pi.ThumbnailPath 
                    FROM Products p 
                    INNER JOIN ProductImages pi ON p.Id = pi.ProductId 
                    WHERE p.SubCategoryId = sc.Id AND p.IsActive = 1
                    ORDER BY pi.IsMainImage DESC, p.Id, pi.DisplayOrder) as FirstProductImage,
                   (SELECT COUNT(*) 
                    FROM Products p 
                    WHERE p.SubCategoryId = sc.Id AND p.IsActive = 1) as ProductCount
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            WHERE c.Slug = @CategorySlug AND sc.IsActive = 1 AND c.IsActive = 1
            ORDER BY sc.DisplayOrder, sc.Name";
        
        using var connection = context.CreateConnection();
        var subCategories = await connection.QueryAsync<SubCategory>(query, new { CategorySlug = categorySlug });
        
        // Cache'e kaydet
        await cacheService.SetSubCategoriesAsync(subCategories);
        
        return subCategories;
    }

    public async Task<SubCategory?> GetBySlugAsync(string slug)
    {
        // Önce cache'den kontrol et
        var cachedSubCategory = await cacheService.GetSubCategoryBySlugAsync(slug);
        if (cachedSubCategory != null)
        {
            return cachedSubCategory;
        }

        var query = @"
            SELECT sc.*, c.Name as CategoryName, c.Slug as CategorySlug
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            WHERE sc.Slug = @Slug AND sc.IsActive = 1";
        
        using var connection = context.CreateConnection();
        var subCategory = await connection.QueryFirstOrDefaultAsync<SubCategory>(query, new { Slug = slug });
        
        if (subCategory != null)
        {
            // Cache'e kaydet
            await cacheService.SetSubCategoryAsync(subCategory);
        }
        
        return subCategory;
    }

    public async Task<SubCategory?> GetByIdAsync(int id)
    {
        // Önce cache'den kontrol et
        var cachedSubCategory = await cacheService.GetSubCategoryByIdAsync(id);
        if (cachedSubCategory != null)
        {
            return cachedSubCategory;
        }

        var query = @"
            SELECT sc.*, c.Name as CategoryName 
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            WHERE sc.Id = @Id";
        
        using var connection = context.CreateConnection();
        var subCategory = await connection.QueryFirstOrDefaultAsync<SubCategory>(query, new { Id = id });
        
        if (subCategory != null)
        {
            // Cache'e kaydet
            await cacheService.SetSubCategoryAsync(subCategory);
        }
        
        return subCategory;
    }

    public async Task<int> CreateAsync(SubCategory subCategory)
    {
        var query = @"
            INSERT INTO SubCategories (CategoryId, Name, Slug, Description, IsActive, DisplayOrder, CreatedAt)
            VALUES (@CategoryId, @Name, @Slug, @Description, @IsActive, @DisplayOrder, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        var newId = await connection.QuerySingleAsync<int>(query, subCategory);
        
        // Cache'i temizle ve yeniden yükle
        await cacheService.InvalidateSubCategoriesAsync();
        
        // Tüm alt kategorileri yeniden yükle ve cache'e kaydet
        var allSubCategories = await GetAllSubCategoriesFromDatabaseAsync();
        await cacheService.SetSubCategoriesAsync(allSubCategories);
        
        return newId;
    }

    public async Task<int> UpdateAsync(SubCategory subCategory)
    {
        // Önce eski veriyi çek (eski slug için)
        var oldSubCategory = await GetByIdAsync(subCategory.Id);
        var oldSlug = oldSubCategory?.Slug;
        
        var query = @"
            UPDATE SubCategories 
            SET CategoryId = @CategoryId,
                Name = @Name,
                Slug = @Slug,
                Description = @Description, 
                IsActive = @IsActive,
                DisplayOrder = @DisplayOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, subCategory);
        
        if (result > 0)
        {
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateSubCategoriesAsync();
            await cacheService.InvalidateSubCategoryAsync(subCategory.Id);
            await cacheService.InvalidateSubCategoryBySlugAsync(subCategory.Slug);
            
            // Eski slug varsa onu da temizle
            if (!string.IsNullOrEmpty(oldSlug) && oldSlug != subCategory.Slug)
            {
                await cacheService.InvalidateSubCategoryBySlugAsync(oldSlug);
            }
            
            // Tüm alt kategorileri yeniden yükle ve cache'e kaydet
            var allSubCategories = await GetAllSubCategoriesFromDatabaseAsync();
            await cacheService.SetSubCategoriesAsync(allSubCategories);
        }
        
        return result;
    }

    public async Task<int> DeleteAsync(int id)
    {
        var query = "DELETE FROM SubCategories WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id });
        
        if (result > 0)
        {
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateSubCategoriesAsync();
            await cacheService.InvalidateSubCategoryAsync(id);
            
            // Tüm alt kategorileri yeniden yükle ve cache'e kaydet
            var allSubCategories = await GetAllSubCategoriesFromDatabaseAsync();
            await cacheService.SetSubCategoriesAsync(allSubCategories);
        }
        
        return result;
    }

    public async Task<IEnumerable<SubCategory>> SearchAsync(string query, int limit = 10)
    {
        var sql = @"
            SELECT TOP (@Limit) sc.*, c.Name as CategoryName, c.Slug as CategorySlug
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            WHERE sc.IsActive = 1 AND c.IsActive = 1
            AND (sc.Name LIKE @SearchQuery OR sc.Description LIKE @SearchQuery OR c.Name LIKE @SearchQuery)
            ORDER BY 
                CASE 
                    WHEN sc.Name LIKE @ExactQuery THEN 1
                    WHEN c.Name LIKE @ExactQuery THEN 2
                    WHEN sc.Description LIKE @ExactQuery THEN 3
                    ELSE 4
                END,
                sc.Name";
        
        var searchQuery = $"%{query}%";
        var exactQuery = $"{query}%";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<SubCategory>(sql, new { SearchQuery = searchQuery, ExactQuery = exactQuery, Limit = limit });
    }

    private async Task<IEnumerable<SubCategory>> GetAllSubCategoriesFromDatabaseAsync()
    {
        var query = @"
            SELECT sc.*, c.Name as CategoryName 
            FROM SubCategories sc
            INNER JOIN Categories c ON sc.CategoryId = c.Id
            ORDER BY c.DisplayOrder, c.Name, sc.DisplayOrder, sc.Name";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<SubCategory>(query);
    }

    // Sıralama Metodları
    public async Task<bool> UpdateDisplayOrderAsync(int id, int newDisplayOrder)
    {
        var query = @"
            UPDATE SubCategories 
            SET DisplayOrder = @DisplayOrder,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id, DisplayOrder = newDisplayOrder, UpdatedAt = DateTime.Now });
        
        if (result > 0)
        {
            await cacheService.InvalidateSubCategoriesAsync();
        }
        
        return result > 0;
    }

    public async Task<bool> ReorderSubCategoriesAsync(Dictionary<int, int> orderMap)
    {
        using var connection = context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            foreach (var (subCategoryId, displayOrder) in orderMap)
            {
                var query = "UPDATE SubCategories SET DisplayOrder = @DisplayOrder, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                await connection.ExecuteAsync(query, new { Id = subCategoryId, DisplayOrder = displayOrder, UpdatedAt = DateTime.Now }, transaction);
            }
            
            transaction.Commit();
            await cacheService.InvalidateSubCategoriesAsync();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }
}

