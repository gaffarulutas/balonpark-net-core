using Dapper;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Data;

public class ProductImageRepository(DapperContext context, IWebHostEnvironment environment, ICacheService cacheService)
{

    public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId)
    {
        var query = "SELECT * FROM ProductImages WHERE ProductId = @ProductId ORDER BY DisplayOrder, Id";
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<ProductImage>(query, new { ProductId = productId });
    }

    public async Task<ProductImage?> GetMainImageAsync(int productId)
    {
        var query = "SELECT TOP 1 * FROM ProductImages WHERE ProductId = @ProductId AND IsMainImage = 1";
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProductImage>(query, new { ProductId = productId });
    }

    /// <summary>
    /// Verilen ürün ID'leri için ana resimleri tek sorguda getirir (karşılaştırma sayfası N+1 önlemi).
    /// </summary>
    public async Task<Dictionary<int, ProductImage>> GetMainImagesByProductIdsAsync(IEnumerable<int> productIds)
    {
        var idList = productIds?.Distinct().ToList() ?? new List<int>();
        if (idList.Count == 0)
            return new Dictionary<int, ProductImage>();

        var query = @"SELECT * FROM ProductImages WHERE ProductId IN @ProductIds AND IsMainImage = 1";
        using var connection = context.CreateConnection();
        var images = await connection.QueryAsync<ProductImage>(query, new { ProductIds = idList });
        return images.ToDictionary(i => i.ProductId, i => i);
    }

    public async Task<int> CreateAsync(ProductImage image)
    {
        var query = @"
            INSERT INTO ProductImages (ProductId, FileName, OriginalPath, LargePath, ThumbnailPath, IsMainImage, DisplayOrder, CreatedAt)
            VALUES (@ProductId, @FileName, @OriginalPath, @LargePath, @ThumbnailPath, @IsMainImage, @DisplayOrder, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        var newId = await connection.QuerySingleAsync<int>(query, image);
        
        // Ürün cache'ini temizle (resim değişti)
        await cacheService.InvalidateProductAsync(image.ProductId);
        await cacheService.InvalidateProductsAsync();
        
        return newId;
    }

    public async Task<int> SetMainImageAsync(int productId, int imageId)
    {
        using var connection = context.CreateConnection();
        
        // Önce tüm resimleri ana resim olmaktan çıkar
        var clearQuery = "UPDATE ProductImages SET IsMainImage = 0 WHERE ProductId = @ProductId";
        await connection.ExecuteAsync(clearQuery, new { ProductId = productId });
        
        // Sonra seçilen resmi ana resim yap
        var setQuery = "UPDATE ProductImages SET IsMainImage = 1 WHERE Id = @ImageId AND ProductId = @ProductId";
        var result = await connection.ExecuteAsync(setQuery, new { ProductId = productId, ImageId = imageId });
        
        // Ürün cache'ini temizle (ana resim değişti)
        if (result > 0)
        {
            await cacheService.InvalidateProductAsync(productId);
            await cacheService.InvalidateProductsAsync();
        }
        
        return result;
    }

    public async Task<ProductImage?> GetByIdAsync(int id)
    {
        var query = "SELECT * FROM ProductImages WHERE Id = @Id";
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProductImage>(query, new { Id = id });
    }

    public async Task<int> DeleteAsync(int id)
    {
        // Önce resim bilgilerini al
        var image = await GetByIdAsync(id);
        if (image == null)
            return 0;

        // Veritabanından sil
        var query = "DELETE FROM ProductImages WHERE Id = @Id";
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, new { Id = id });

        // Dosyaları sil
        if (result > 0)
        {
            var basePath = environment.WebRootPath;
            var originalPath = Path.Combine(basePath, image.OriginalPath);
            var largePath = Path.Combine(basePath, image.LargePath);
            var thumbnailPath = Path.Combine(basePath, image.ThumbnailPath);

            try
            {
                ImageHelper.DeleteProductImages(originalPath, largePath, thumbnailPath);
            }
            catch (Exception ex)
            {
                // Dosya silme hatası loglanabilir ama işlemi durdurmaz
                Console.WriteLine($"Dosya silme hatası: {ex.Message}");
            }
            
            // Ürün cache'ini temizle (resim silindi)
            await cacheService.InvalidateProductAsync(image.ProductId);
            await cacheService.InvalidateProductsAsync();
        }

        return result;
    }

}

