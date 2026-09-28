using Microsoft.Extensions.Options;

namespace BalonPark.Services.CatalogSync;

public sealed class CatalogSyncHostedService(
    ICatalogSyncQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogSyncOptions> options,
    ILogger<CatalogSyncHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("CatalogSync hosted service idle (Enabled=false)");
            // Still drain nothing meaningful; keep alive so Enqueue no-ops stay cheap.
        }
        else
        {
            logger.LogInformation("CatalogSync hosted service started with {Count} targets",
                options.Value.Targets.Count);
        }

        await foreach (var job in queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            if (!options.Value.Enabled)
                continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ICatalogSyncProcessor>();
                await processor.ProcessAsync(job, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CatalogSync worker failed on {EntityType} {Operation} Id={Id}",
                    job.EntityType, job.Operation, job.EntityId);
            }
        }
    }
}
