using BalonPark.Data;
using Microsoft.Extensions.Options;

namespace BalonPark.Services.CatalogSync;

public interface ICatalogSyncProcessor
{
    Task ProcessAsync(CatalogSyncJob job, CancellationToken cancellationToken = default);
}

public sealed class CatalogSyncProcessor(
    IOptions<CatalogSyncOptions> options,
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    ProductRepository productRepository,
    ProductImageRepository productImageRepository,
    ICatalogTranslationService translationService,
    ITargetCatalogWriter targetWriter,
    ITargetUploadSync uploadSync,
    ILogger<CatalogSyncProcessor> logger) : ICatalogSyncProcessor
{
    public async Task ProcessAsync(CatalogSyncJob job, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Enabled || opts.Targets.Count == 0)
            return;

        logger.LogInformation("CatalogSync processing {EntityType} {Operation} Id={Id}", job.EntityType, job.Operation, job.EntityId);

        foreach (var target in opts.Targets)
        {
            if (string.IsNullOrWhiteSpace(target.ConnectionString))
            {
                logger.LogWarning("CatalogSync target {Name} skipped: no connection string", target.Name);
                continue;
            }

            try
            {
                await ProcessTargetAsync(job, target, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("CatalogSync OK {Target} {EntityType} {Operation} Id={Id}",
                    target.Name, job.EntityType, job.Operation, job.EntityId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CatalogSync ERR {Target} {EntityType} {Operation} Id={Id}",
                    target.Name, job.EntityType, job.Operation, job.EntityId);
            }
        }
    }

    private async Task ProcessTargetAsync(CatalogSyncJob job, CatalogSyncTargetOptions target, CancellationToken cancellationToken)
    {
        switch (job.EntityType)
        {
            case CatalogSyncEntityType.Category:
                if (job.Operation == CatalogSyncOperation.Delete)
                {
                    await targetWriter.DeleteCategoryAsync(target, job.EntityId, cancellationToken).ConfigureAwait(false);
                    return;
                }
                var category = await categoryRepository.GetByIdAsync(job.EntityId).ConfigureAwait(false);
                if (category is null)
                {
                    logger.LogWarning("CatalogSync: category {Id} missing on source", job.EntityId);
                    return;
                }
                var translatedCategory = await translationService.TranslateCategoryAsync(category, target.Locale, cancellationToken).ConfigureAwait(false);
                translatedCategory.UpdatedAt = DateTime.Now;
                await targetWriter.UpsertCategoryAsync(target, translatedCategory, cancellationToken).ConfigureAwait(false);
                break;

            case CatalogSyncEntityType.SubCategory:
                if (job.Operation == CatalogSyncOperation.Delete)
                {
                    await targetWriter.DeleteSubCategoryAsync(target, job.EntityId, cancellationToken).ConfigureAwait(false);
                    return;
                }
                var sub = await subCategoryRepository.GetByIdAsync(job.EntityId).ConfigureAwait(false);
                if (sub is null)
                {
                    logger.LogWarning("CatalogSync: subcategory {Id} missing on source", job.EntityId);
                    return;
                }
                var translatedSub = await translationService.TranslateSubCategoryAsync(sub, target.Locale, cancellationToken).ConfigureAwait(false);
                translatedSub.UpdatedAt = DateTime.Now;
                await targetWriter.UpsertSubCategoryAsync(target, translatedSub, cancellationToken).ConfigureAwait(false);
                break;

            case CatalogSyncEntityType.Product:
                if (job.Operation == CatalogSyncOperation.Delete)
                {
                    await targetWriter.DeleteProductAsync(target, job.EntityId, cancellationToken).ConfigureAwait(false);
                    await uploadSync.DeleteProductFolderAsync(target, job.EntityId, cancellationToken).ConfigureAwait(false);
                    return;
                }
                var product = await productRepository.GetByIdAsync(job.EntityId).ConfigureAwait(false);
                if (product is null)
                {
                    logger.LogWarning("CatalogSync: product {Id} missing on source", job.EntityId);
                    return;
                }
                var images = (await productImageRepository.GetByProductIdAsync(job.EntityId).ConfigureAwait(false)).ToList();
                var translatedProduct = await translationService.TranslateProductAsync(product, target.Locale, cancellationToken).ConfigureAwait(false);
                translatedProduct.UpdatedAt = DateTime.Now;
                await targetWriter.UpsertProductAsync(target, translatedProduct, images, cancellationToken).ConfigureAwait(false);
                await uploadSync.SyncProductFolderAsync(target, job.EntityId, cancellationToken).ConfigureAwait(false);
                break;

            default:
                logger.LogWarning("CatalogSync: unknown entity type {Type}", job.EntityType);
                break;
        }
    }
}
