namespace BalonPark.Services.CatalogSync;

public enum CatalogSyncEntityType
{
    Category = 1,
    SubCategory = 2,
    Product = 3
}

public enum CatalogSyncOperation
{
    Upsert = 1,
    Delete = 2
}

public sealed class CatalogSyncJob
{
    public CatalogSyncEntityType EntityType { get; init; }
    public CatalogSyncOperation Operation { get; init; }
    public int EntityId { get; init; }
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
}
