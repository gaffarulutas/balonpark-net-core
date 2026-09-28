using BalonPark.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BalonPark.Services.CatalogSync;

public interface ITargetCatalogWriter
{
    Task UpsertCategoryAsync(CatalogSyncTargetOptions target, Category category, CancellationToken cancellationToken = default);
    Task UpsertSubCategoryAsync(CatalogSyncTargetOptions target, SubCategory subCategory, CancellationToken cancellationToken = default);
    Task UpsertProductAsync(CatalogSyncTargetOptions target, Product product, IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default);
    Task DeleteSubCategoryAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default);
}

public sealed class TargetCatalogWriter(ILogger<TargetCatalogWriter> logger) : ITargetCatalogWriter
{
    public Task UpsertCategoryAsync(CatalogSyncTargetOptions target, Category category, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            category.Slug = await EnsureUniqueSlugAsync(conn, "Categories", category.Slug, category.Id, 200, cancellationToken).ConfigureAwait(false);
            var exists = await ExistsAsync(conn, "Categories", category.Id, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                await conn.ExecuteAsync(new CommandDefinition("""
                    UPDATE Categories SET Name=@Name, Slug=@Slug, Description=@Description, IsActive=@IsActive,
                        DisplayOrder=@DisplayOrder, UpdatedAt=@UpdatedAt
                    WHERE Id=@Id
                    """, category, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT Categories ON", cancellationToken: cancellationToken)).ConfigureAwait(false);
                try
                {
                    await conn.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO Categories (Id, Name, Slug, Description, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
                        VALUES (@Id, @Name, @Slug, @Description, @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt)
                        """, category, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
                finally
                {
                    await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT Categories OFF", cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }
        }, cancellationToken);

    public Task UpsertSubCategoryAsync(CatalogSyncTargetOptions target, SubCategory subCategory, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            subCategory.Slug = await EnsureUniqueSlugAsync(conn, "SubCategories", subCategory.Slug, subCategory.Id, 200, cancellationToken).ConfigureAwait(false);
            var exists = await ExistsAsync(conn, "SubCategories", subCategory.Id, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                await conn.ExecuteAsync(new CommandDefinition("""
                    UPDATE SubCategories SET CategoryId=@CategoryId, Name=@Name, Slug=@Slug, Description=@Description,
                        IsActive=@IsActive, DisplayOrder=@DisplayOrder, UpdatedAt=@UpdatedAt
                    WHERE Id=@Id
                    """, subCategory, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT SubCategories ON", cancellationToken: cancellationToken)).ConfigureAwait(false);
                try
                {
                    await conn.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO SubCategories (Id, CategoryId, Name, Slug, Description, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
                        VALUES (@Id, @CategoryId, @Name, @Slug, @Description, @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt)
                        """, subCategory, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
                finally
                {
                    await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT SubCategories OFF", cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }
        }, cancellationToken);

    public Task UpsertProductAsync(CatalogSyncTargetOptions target, Product product, IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            product.Slug = await EnsureUniqueSlugAsync(conn, "Products", product.Slug, product.Id, 200, cancellationToken, tx).ConfigureAwait(false);
            var exists = await ExistsAsync(conn, "Products", product.Id, cancellationToken, tx).ConfigureAwait(false);

            if (exists)
            {
                await conn.ExecuteAsync(new CommandDefinition(ProductUpdateSql, product, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT Products ON", transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                try
                {
                    await conn.ExecuteAsync(new CommandDefinition(ProductInsertSql, product, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
                finally
                {
                    await conn.ExecuteAsync(new CommandDefinition("SET IDENTITY_INSERT Products OFF", transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }

            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM ProductImages WHERE ProductId=@ProductId",
                new { ProductId = product.Id },
                transaction: tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (images.Count > 0)
            {
                foreach (var image in images)
                {
                    await conn.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO ProductImages (ProductId, FileName, OriginalPath, LargePath, ThumbnailPath, IsMainImage, DisplayOrder, CreatedAt)
                        VALUES (@ProductId, @FileName, @OriginalPath, @LargePath, @ThumbnailPath, @IsMainImage, @DisplayOrder, @CreatedAt)
                        """, image, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);

    public Task DeleteCategoryAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            // Soft-avoid FK blowups: deactivate first; hard-delete only if no children remain.
            var childCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM SubCategories WHERE CategoryId=@Id",
                new { Id = id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (childCount > 0)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE Categories SET IsActive=0, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id",
                    new { Id = id },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
                return;
            }
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Categories WHERE Id=@Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }, cancellationToken);

    public Task DeleteSubCategoryAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            var childCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM Products WHERE SubCategoryId=@Id",
                new { Id = id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (childCount > 0)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE SubCategories SET IsActive=0, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id",
                    new { Id = id },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
                return;
            }
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM SubCategories WHERE Id=@Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }, cancellationToken);

    public Task DeleteProductAsync(CatalogSyncTargetOptions target, int id, CancellationToken cancellationToken = default)
        => WithRetry(target, async () =>
        {
            await using var conn = CreateConnection(target);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM ProductImages WHERE ProductId=@Id", new { Id = id }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM Products WHERE Id=@Id", new { Id = id }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);

    private async Task WithRetry(CatalogSyncTargetOptions target, Func<Task> action, CancellationToken cancellationToken = default)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                logger.LogWarning(ex, "CatalogSync target {Target} attempt {Attempt} failed", target.Name, attempt);
                if (attempt < 3)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 * attempt, 8)), cancellationToken).ConfigureAwait(false);
            }
        }
        throw new InvalidOperationException($"CatalogSync target write failed: {target.Name}", last);
    }

    private static SqlConnection CreateConnection(CatalogSyncTargetOptions target)
        => new(target.ConnectionString);

    private static async Task<bool> ExistsAsync(
        SqlConnection conn, string table, int id, CancellationToken cancellationToken, SqlTransaction? tx = null)
    {
        var n = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(1) FROM [{table}] WHERE Id=@Id",
            new { Id = id },
            transaction: tx,
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    private static async Task<string> EnsureUniqueSlugAsync(
        SqlConnection conn,
        string table,
        string slug,
        int rowId,
        int maxLen,
        CancellationToken cancellationToken,
        SqlTransaction? tx = null)
    {
        var baseSlug = string.IsNullOrWhiteSpace(slug) ? $"item-{rowId}" : slug.Trim();
        if (baseSlug.Length > maxLen)
            baseSlug = baseSlug[..maxLen];

        var candidate = baseSlug;
        for (var ntry = 0; ntry < 30; ntry++)
        {
            var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                $"SELECT COUNT(1) FROM [{table}] WHERE Slug=@Slug AND Id<>@Id",
                new { Slug = candidate, Id = rowId },
                transaction: tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (count == 0)
                return candidate;

            var suffix = ntry == 0 ? $"-{rowId}" : $"-{rowId}-{ntry}";
            var cut = Math.Max(1, maxLen - suffix.Length);
            candidate = (baseSlug[..Math.Min(baseSlug.Length, cut)] + suffix);
            if (candidate.Length > maxLen)
                candidate = candidate[..maxLen];
        }

        var fallback = $"-{rowId}";
        return (baseSlug[..Math.Max(1, maxLen - fallback.Length)] + fallback)[..maxLen];
    }

    private const string ProductInsertSql = """
        INSERT INTO Products (
            Id, CategoryId, SubCategoryId, Name, Slug, Description, TechnicalDescription, Summary,
            Price, Stock, DisplayOrder, IsActive, CreatedAt, UpdatedAt, ViewCount,
            InflatedLength, InflatedWidth, InflatedHeight, UserCount, AssemblyTime, RequiredPersonCount,
            FanDescription, FanWeightKg, PackagedLength, PackagedDepth, PackagedWeightKg, PackagePalletCount,
            HasCertificate, WarrantyDescription, AfterSalesService,
            IsDiscounted, IsPopular, IsProjectSpecial, DeliveryDays, DeliveryDaysMin, DeliveryDaysMax,
            IsFireResistant, MaterialWeight, MaterialWeightGrm2, ColorOptions, InflatedWeightKg)
        VALUES (
            @Id, @CategoryId, @SubCategoryId, @Name, @Slug, @Description, @TechnicalDescription, @Summary,
            @Price, @Stock, @DisplayOrder, @IsActive, @CreatedAt, @UpdatedAt, @ViewCount,
            @InflatedLength, @InflatedWidth, @InflatedHeight, @UserCount, @AssemblyTime, @RequiredPersonCount,
            @FanDescription, @FanWeightKg, @PackagedLength, @PackagedDepth, @PackagedWeightKg, @PackagePalletCount,
            @HasCertificate, @WarrantyDescription, @AfterSalesService,
            @IsDiscounted, @IsPopular, @IsProjectSpecial, @DeliveryDays, @DeliveryDaysMin, @DeliveryDaysMax,
            @IsFireResistant, @MaterialWeight, @MaterialWeightGrm2, @ColorOptions, @InflatedWeightKg)
        """;

    private const string ProductUpdateSql = """
        UPDATE Products SET
            CategoryId=@CategoryId, SubCategoryId=@SubCategoryId, Name=@Name, Slug=@Slug,
            Description=@Description, TechnicalDescription=@TechnicalDescription, Summary=@Summary,
            Price=@Price, Stock=@Stock, DisplayOrder=@DisplayOrder, IsActive=@IsActive, UpdatedAt=@UpdatedAt,
            InflatedLength=@InflatedLength, InflatedWidth=@InflatedWidth, InflatedHeight=@InflatedHeight,
            UserCount=@UserCount, AssemblyTime=@AssemblyTime, RequiredPersonCount=@RequiredPersonCount,
            FanDescription=@FanDescription, FanWeightKg=@FanWeightKg,
            PackagedLength=@PackagedLength, PackagedDepth=@PackagedDepth, PackagedWeightKg=@PackagedWeightKg,
            PackagePalletCount=@PackagePalletCount, HasCertificate=@HasCertificate,
            WarrantyDescription=@WarrantyDescription, AfterSalesService=@AfterSalesService,
            IsDiscounted=@IsDiscounted, IsPopular=@IsPopular, IsProjectSpecial=@IsProjectSpecial,
            DeliveryDays=@DeliveryDays, DeliveryDaysMin=@DeliveryDaysMin, DeliveryDaysMax=@DeliveryDaysMax,
            IsFireResistant=@IsFireResistant, MaterialWeight=@MaterialWeight, MaterialWeightGrm2=@MaterialWeightGrm2,
            ColorOptions=@ColorOptions, InflatedWeightKg=@InflatedWeightKg
        WHERE Id=@Id
        """;
}
