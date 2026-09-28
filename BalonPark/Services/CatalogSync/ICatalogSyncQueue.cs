namespace BalonPark.Services.CatalogSync;

public interface ICatalogSyncQueue
{
    ValueTask EnqueueAsync(CatalogSyncJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<CatalogSyncJob> ReadAllAsync(CancellationToken cancellationToken);
}
