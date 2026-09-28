using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace BalonPark.Services.CatalogSync;

public sealed class CatalogSyncQueue(IOptions<CatalogSyncOptions> options, ILogger<CatalogSyncQueue> logger) : ICatalogSyncQueue
{
    private readonly Channel<CatalogSyncJob> _channel = Channel.CreateUnbounded<CatalogSyncJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    // Coalesce rapid edits: keep only the latest job per entity key until the worker dequeues.
    private readonly object _pendingLock = new();
    private readonly Dictionary<string, CatalogSyncJob> _pending = new(StringComparer.Ordinal);
    private readonly HashSet<string> _queuedKeys = new(StringComparer.Ordinal);

    public ValueTask EnqueueAsync(CatalogSyncJob job, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return ValueTask.CompletedTask;

        if (job.EntityId <= 0)
        {
            logger.LogWarning("CatalogSync: ignored job with invalid EntityId ({EntityType}/{Operation})", job.EntityType, job.Operation);
            return ValueTask.CompletedTask;
        }

        var key = $"{job.EntityType}:{job.EntityId}";
        lock (_pendingLock)
        {
            // Delete always wins over a stale Upsert for the same entity.
            if (_pending.TryGetValue(key, out var existing)
                && existing.Operation == CatalogSyncOperation.Delete
                && job.Operation == CatalogSyncOperation.Upsert)
            {
                logger.LogInformation("CatalogSync: drop Upsert after pending Delete for {Key}", key);
                return ValueTask.CompletedTask;
            }

            _pending[key] = job;
            if (!_queuedKeys.Add(key))
            {
                logger.LogInformation("CatalogSync: coalesced {EntityType} {Operation} Id={Id}", job.EntityType, job.Operation, job.EntityId);
                return ValueTask.CompletedTask;
            }
        }

        if (!_channel.Writer.TryWrite(new CatalogSyncJob
            {
                EntityType = job.EntityType,
                Operation = job.Operation,
                EntityId = job.EntityId,
                EnqueuedAt = job.EnqueuedAt
            }))
        {
            lock (_pendingLock)
            {
                _queuedKeys.Remove(key);
                _pending.Remove(key);
            }
            logger.LogError("CatalogSync: failed to enqueue {EntityType} {Operation} Id={Id}", job.EntityType, job.Operation, job.EntityId);
        }
        else
        {
            logger.LogInformation("CatalogSync: enqueued {EntityType} {Operation} Id={Id}", job.EntityType, job.Operation, job.EntityId);
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<CatalogSyncJob> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var marker in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = $"{marker.EntityType}:{marker.EntityId}";
            CatalogSyncJob job;
            lock (_pendingLock)
            {
                if (!_pending.TryGetValue(key, out job!))
                    job = marker;
                _pending.Remove(key);
                _queuedKeys.Remove(key);
            }
            yield return job;
        }
    }
}
