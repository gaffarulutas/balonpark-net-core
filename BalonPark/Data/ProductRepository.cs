using Dapper;
using BalonPark.Models;
using BalonPark.Services;
using BalonPark.Helpers;
using Microsoft.Extensions.Logging;

namespace BalonPark.Data;

public class ProductRepository(DapperContext context, ICacheService cacheService, IWebHostEnvironment environment, ILogger<ProductRepository> logger)
{

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        // Önce cache'den kontrol et
        var cachedProducts = await cacheService.GetProductsAsync();
        if (cachedProducts != null)
        {
            return cachedProducts;
        }

        var query = @"
            SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            ORDER BY p.CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        var products = await connection.QueryAsync<Product>(query);
        
        // Cache'e kaydet
        await cacheService.SetProductsAsync(products);
        
        return products;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        // Önce cache'den kontrol et
        var cachedProduct = await cacheService.GetProductByIdAsync(id);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }

        var query = @"
            SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            WHERE p.Id = @Id";
        
        using var connection = context.CreateConnection();
        var product = await connection.QueryFirstOrDefaultAsync<Product>(query, new { Id = id });
        
        if (product != null)
        {
            // Cache'e kaydet
            await cacheService.SetProductAsync(product);
        }
        
        return product;
    }

    public async Task<Product?> GetBySlugAsync(string slug)
    {
        // Önce cache'den kontrol et
        var cachedProduct = await cacheService.GetProductBySlugAsync(slug);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }

        var query = @"
            SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            WHERE p.Slug = @Slug AND p.IsActive = 1";
        
        using var connection = context.CreateConnection();
        var product = await connection.QueryFirstOrDefaultAsync<Product>(query, new { Slug = slug });
        
        if (product != null)
        {
            // Cache'e kaydet
            await cacheService.SetProductAsync(product);
        }
        
        return product;
    }

    /// <summary>
    /// Slug listesine göre ürünleri tek sorguda getirir (karşılaştırma sayfası N+1 önlemi).
    /// Dönen sıra slug listesi sırasına göredir.
    /// </summary>
    public async Task<IEnumerable<Product>> GetBySlugsAsync(IReadOnlyList<string> slugs)
    {
        if (slugs == null || slugs.Count == 0)
            return Enumerable.Empty<Product>();

        var slugList = slugs.Distinct().ToList();
        var query = @"
            SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            WHERE p.Slug IN @Slugs AND p.IsActive = 1";

        using var connection = context.CreateConnection();
        var products = (await connection.QueryAsync<Product>(query, new { Slugs = slugList })).ToList();
        // URL sırasını koru
        var order = slugList.Select((s, i) => new { s, i }).ToDictionary(x => x.s, x => x.i);
        return products.OrderBy(p => order.TryGetValue(p.Slug, out var idx) ? idx : int.MaxValue);
    }

    public async Task<IEnumerable<Product>> GetBySubCategorySlugAsync(string subCategorySlug)
    {
        try
        {
            var query = @"
                SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                       sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
                WHERE sc.Slug = @SubCategorySlug AND p.IsActive = 1 AND sc.IsActive = 1 AND c.IsActive = 1
                ORDER BY p.DisplayOrder ASC, p.Name ASC";
            
            using var connection = context.CreateConnection();
            return await connection.QueryAsync<Product>(query, new { SubCategorySlug = subCategorySlug });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Alt kategori ürünleri getirme hatası. SubCategorySlug: {SubCategorySlug}", subCategorySlug);
            return Enumerable.Empty<Product>();
        }
    }

    public async Task<IEnumerable<Product>> GetByCategorySlugAsync(string categorySlug)
    {
        try
        {
            var query = @"
                SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                       sc.Name as SubCategoryName, sc.Slug as SubCategorySlug,
                       sc.DisplayOrder as SubCategoryDisplayOrder
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
                WHERE c.Slug = @CategorySlug AND p.IsActive = 1 AND sc.IsActive = 1 AND c.IsActive = 1
                ORDER BY sc.DisplayOrder ASC, sc.Name ASC, p.DisplayOrder ASC, p.Name ASC";
            
            using var connection = context.CreateConnection();
            return await connection.QueryAsync<Product>(query, new { CategorySlug = categorySlug });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kategori ürünleri getirme hatası. CategorySlug: {CategorySlug}", categorySlug);
            return Enumerable.Empty<Product>();
        }
    }

    public async Task<IEnumerable<Product>> SearchAsync(string query, int limit = 10)
    {
        var sql = @"
            SELECT TOP (@Limit) p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            WHERE p.IsActive = 1 AND sc.IsActive = 1 AND c.IsActive = 1
            AND (p.Name LIKE @SearchQuery 
                 OR p.Description LIKE @SearchQuery 
                 OR c.Name LIKE @SearchQuery 
                 OR sc.Name LIKE @SearchQuery)
            ORDER BY 
                CASE 
                    WHEN p.Name LIKE @ExactQuery THEN 1
                    WHEN c.Name LIKE @ExactQuery THEN 2
                    WHEN sc.Name LIKE @ExactQuery THEN 3
                    WHEN p.Description LIKE @ExactQuery THEN 4
                    ELSE 5
                END,
                p.CreatedAt DESC";
        
        var searchQuery = $"%{query}%";
        var exactQuery = $"{query}%";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Product>(sql, new { SearchQuery = searchQuery, ExactQuery = exactQuery, Limit = limit });
    }

    public async Task<int> CreateAsync(Product product)
    {
        var query = @"
            INSERT INTO Products (CategoryId, SubCategoryId, Name, Slug, Description, TechnicalDescription, Summary, Price, Stock, DisplayOrder, IsActive, CreatedAt,
                InflatedLength, InflatedWidth, InflatedHeight, UserCount, AssemblyTime, RequiredPersonCount, FanDescription, FanWeightKg,
                PackagedLength, PackagedDepth, PackagedWeightKg, PackagePalletCount, HasCertificate, WarrantyDescription, AfterSalesService,
                IsDiscounted, IsPopular, IsProjectSpecial, DeliveryDays, DeliveryDaysMin, DeliveryDaysMax, IsFireResistant, MaterialWeight, MaterialWeightGrm2, ColorOptions, InflatedWeightKg)
            VALUES (@CategoryId, @SubCategoryId, @Name, @Slug, @Description, @TechnicalDescription, @Summary, @Price, @Stock, @DisplayOrder, @IsActive, @CreatedAt,
                @InflatedLength, @InflatedWidth, @InflatedHeight, @UserCount, @AssemblyTime, @RequiredPersonCount, @FanDescription, @FanWeightKg,
                @PackagedLength, @PackagedDepth, @PackagedWeightKg, @PackagePalletCount, @HasCertificate, @WarrantyDescription, @AfterSalesService,
                @IsDiscounted, @IsPopular, @IsProjectSpecial, @DeliveryDays, @DeliveryDaysMin, @DeliveryDaysMax, @IsFireResistant, @MaterialWeight, @MaterialWeightGrm2, @ColorOptions, @InflatedWeightKg);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        var newId = await connection.QuerySingleAsync<int>(query, product);
        
        // Cache'i temizle ve yeniden yükle
        await cacheService.InvalidateProductsAsync();
        
        // Tüm ürünleri yeniden yükle ve cache'e kaydet
        var allProducts = await GetAllProductsFromDatabaseAsync();
        await cacheService.SetProductsAsync(allProducts);
        
        return newId;
    }

    public async Task<int> UpdateAsync(Product product)
    {
        // Önce eski veriyi çek (eski slug için)
        var oldProduct = await GetByIdAsync(product.Id);
        var oldSlug = oldProduct?.Slug;
        
        var query = @"
            UPDATE Products 
            SET CategoryId = @CategoryId,
                SubCategoryId = @SubCategoryId,
                Name = @Name,
                Slug = @Slug,
                Description = @Description,
                TechnicalDescription = @TechnicalDescription,
                Summary = @Summary,
                Price = @Price,
                Stock = @Stock,
                DisplayOrder = @DisplayOrder,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                InflatedLength = @InflatedLength,
                InflatedWidth = @InflatedWidth,
                InflatedHeight = @InflatedHeight,
                UserCount = @UserCount,
                AssemblyTime = @AssemblyTime,
                RequiredPersonCount = @RequiredPersonCount,
                FanDescription = @FanDescription,
                FanWeightKg = @FanWeightKg,
                PackagedLength = @PackagedLength,
                PackagedDepth = @PackagedDepth,
                PackagedWeightKg = @PackagedWeightKg,
                PackagePalletCount = @PackagePalletCount,
                HasCertificate = @HasCertificate,
                WarrantyDescription = @WarrantyDescription,
                AfterSalesService = @AfterSalesService,
                IsDiscounted = @IsDiscounted,
                IsPopular = @IsPopular,
                IsProjectSpecial = @IsProjectSpecial,
                DeliveryDays = @DeliveryDays,
                DeliveryDaysMin = @DeliveryDaysMin,
                DeliveryDaysMax = @DeliveryDaysMax,
                IsFireResistant = @IsFireResistant,
                MaterialWeight = @MaterialWeight,
                MaterialWeightGrm2 = @MaterialWeightGrm2,
                ColorOptions = @ColorOptions,
                InflatedWeightKg = @InflatedWeightKg
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, product);
        
        if (result > 0)
        {
            // Cache'i temizle
            await cacheService.InvalidateProductsAsync();
            await cacheService.InvalidateProductAsync(product.Id);
            await cacheService.InvalidateProductBySlugAsync(product.Slug);
            
            // Eski slug varsa onu da temizle
            if (!string.IsNullOrEmpty(oldSlug) && oldSlug != product.Slug)
            {
                await cacheService.InvalidateProductBySlugAsync(oldSlug);
            }
            
            // Tüm ürünleri yeniden yükle ve cache'e kaydet
            var allProducts = await GetAllProductsFromDatabaseAsync();
            await cacheService.SetProductsAsync(allProducts);
        }
        
        return result;
    }

    public async Task<int> DeleteAsync(int id)
    {
        // Önce ürünün resimlerini al
        var imagesQuery = "SELECT * FROM ProductImages WHERE ProductId = @ProductId";
        using var connection = context.CreateConnection();
        var images = await connection.QueryAsync<ProductImage>(imagesQuery, new { ProductId = id });
        
        // Ürünü sil
        var query = "DELETE FROM Products WHERE Id = @Id";
        var result = await connection.ExecuteAsync(query, new { Id = id });
        
        if (result > 0)
        {
            // Resim dosyalarını sil
            foreach (var image in images)
            {
                try
                {
                    var basePath = environment.WebRootPath;
                    var originalPath = Path.Combine(basePath, image.OriginalPath);
                    var largePath = Path.Combine(basePath, image.LargePath);
                    var thumbnailPath = Path.Combine(basePath, image.ThumbnailPath);

                    ImageHelper.DeleteProductImages(originalPath, largePath, thumbnailPath);
                }
                catch (Exception ex)
                {
                    // Dosya silme hatası loglanabilir ama işlemi durdurmaz
                    Console.WriteLine($"Resim dosyası silme hatası: {ex.Message}");
                }
            }
            
            // Ürün klasörünü sil (eğer boşsa)
            try
            {
                var productFolder = Path.Combine(environment.WebRootPath, "uploads", "products", id.ToString());
                if (Directory.Exists(productFolder))
                {
                    Directory.Delete(productFolder, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Klasör silme hatası: {ex.Message}");
            }
            
            // Cache'i temizle ve yeniden yükle
            await cacheService.InvalidateProductsAsync();
            await cacheService.InvalidateProductAsync(id);
            
            // Tüm ürünleri yeniden yükle ve cache'e kaydet
            var allProducts = await GetAllProductsFromDatabaseAsync();
            await cacheService.SetProductsAsync(allProducts);
        }
        
        return result;
    }

    public async Task<int> IncrementViewCountAsync(int productId, string? productSlug = null, string? categorySlug = null, string? subCategorySlug = null)
    {
        var query = @"
            UPDATE Products
            SET ViewCount = ViewCount + 1
            WHERE Id = @Id;

            SELECT ViewCount
            FROM Products
            WHERE Id = @Id;";

        using var connection = context.CreateConnection();
        var newCount = await connection.ExecuteScalarAsync<int>(query, new { Id = productId });

        await cacheService.InvalidateProductAsync(productId);
        if (!string.IsNullOrEmpty(productSlug))
        {
            await cacheService.InvalidateProductBySlugAsync(productSlug);
        }
        if (!string.IsNullOrEmpty(categorySlug))
        {
            await cacheService.InvalidateProductsByCategorySlugAsync(categorySlug);
        }
        if (!string.IsNullOrEmpty(subCategorySlug))
        {
            await cacheService.InvalidateProductsBySubCategorySlugAsync(subCategorySlug);
        }
        await cacheService.InvalidateProductsAsync();

        return newCount;
    }

    private async Task<IEnumerable<Product>> GetAllProductsFromDatabaseAsync()
    {
        var query = @"
            SELECT p.*, c.Name as CategoryName, c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, sc.Slug as SubCategorySlug
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            ORDER BY p.CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Product>(query);
    }

    /// <summary>
    /// Google Shopping için tüm aktif ürünleri kategori ve alt kategori bilgileriyle birlikte getirir
    /// Cache kullanmaz, her zaman database'den güncel veriyi alır
    /// </summary>
    public async Task<IEnumerable<Product>> GetAllForGoogleShoppingAsync()
    {
        var query = @"
            SELECT p.*, 
                   c.Name as CategoryName, 
                   c.Slug as CategorySlug,
                   sc.Name as SubCategoryName, 
                   sc.Slug as SubCategorySlug,
                   pi.ThumbnailPath as MainImagePath
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            INNER JOIN SubCategories sc ON p.SubCategoryId = sc.Id
            LEFT JOIN ProductImages pi ON p.Id = pi.ProductId AND pi.IsMainImage = 1
            WHERE p.IsActive = 1 AND p.Price > 0
            ORDER BY p.CreatedAt DESC";
        
        using var connection = context.CreateConnection();
        var products = await connection.QueryAsync<Product>(query);
        
        return products;
    }

    /// <summary>
    /// Belirli bir ürünün tüm resimlerini getirir
    /// </summary>
    public async Task<IEnumerable<ProductImage>> GetProductImagesAsync(int productId)
    {
        var query = @"
            SELECT * FROM ProductImages 
            WHERE ProductId = @ProductId 
            ORDER BY IsMainImage DESC, DisplayOrder, Id";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<ProductImage>(query, new { ProductId = productId });
    }
}

