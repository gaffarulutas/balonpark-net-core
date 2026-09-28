namespace BalonPark.Services.CatalogSync;

/// <summary>Fire-and-forget helper so admin pages stay lean.</summary>
public interface ICatalogSyncPublisher
{
    void PublishUpsert(CatalogSyncEntityType entityType, int entityId);
    void PublishDelete(CatalogSyncEntityType entityType, int entityId);
}

public sealed class CatalogSyncPublisher(ICatalogSyncQueue queue, ILogger<CatalogSyncPublisher> logger) : ICatalogSyncPublisher
{
    public void PublishUpsert(CatalogSyncEntityType entityType, int entityId)
        => Publish(entityType, CatalogSyncOperation.Upsert, entityId);

    public void PublishDelete(CatalogSyncEntityType entityType, int entityId)
        => Publish(entityType, CatalogSyncOperation.Delete, entityId);

    private void Publish(CatalogSyncEntityType entityType, CatalogSyncOperation operation, int entityId)
    {
        try
        {
            var job = new CatalogSyncJob
            {
                EntityType = entityType,
                Operation = operation,
                EntityId = entityId
            };
            // Queue.Enqueue is sync when Enabled=false; otherwise TryWrite is non-blocking.
            _ = queue.EnqueueAsync(job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CatalogSync: failed to publish {EntityType} {Operation} Id={Id}", entityType, operation, entityId);
        }
    }
}
