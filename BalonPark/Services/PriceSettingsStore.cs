using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using BalonPark.Data;
using BalonPark.Helpers;

namespace BalonPark.Services;

public readonly record struct PriceSettingsSnapshot(bool ShowPrices, DateTime Revision);

public interface IPriceSettingsStore
{
    Task<PriceSettingsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task SetShowPricesAsync(bool showPrices, CancellationToken cancellationToken = default);
    Task<int> AdjustPricesAsync(int percent, bool increase, CancellationToken cancellationToken = default);
}

public sealed class PriceSettingsStore(
    DapperContext context,
    IMemoryCache cache,
    ICacheService appCache,
    ILogger<PriceSettingsStore> logger) : IPriceSettingsStore
{
    public const string SnapshotCacheKey = "price_settings_snapshot_v1";
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(15);

    public async Task<PriceSettingsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(SnapshotCacheKey, out PriceSettingsSnapshot cached))
            return cached;

        try
        {
            using var connection = context.CreateConnection();
            if (connection is not SqlConnection sql)
                throw new InvalidOperationException("SQL bağlantısı bekleniyor.");

            await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await PriceSettingsSql.ReadAsync(sql, cancellationToken).ConfigureAwait(false);
            cache.Set(SnapshotCacheKey, snapshot, SnapshotTtl);
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fiyat ayarı okunamadı; fiyatlar açık kabul edildi.");
            return new PriceSettingsSnapshot(true, DateTime.UnixEpoch);
        }
    }

    public async Task SetShowPricesAsync(bool showPrices, CancellationToken cancellationToken = default)
    {
        using var connection = context.CreateConnection();
        if (connection is not SqlConnection sql)
            throw new InvalidOperationException("SQL bağlantısı bekleniyor.");

        await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
        await PriceSettingsSql.SetFlagAsync(sql, showPrices, cancellationToken).ConfigureAwait(false);
        await InvalidateAsync().ConfigureAwait(false);
    }

    public async Task<int> AdjustPricesAsync(int percent, bool increase, CancellationToken cancellationToken = default)
    {
        var factor = PriceAdjustment.Factor(percent, increase);
        using var connection = context.CreateConnection();
        if (connection is not SqlConnection sql)
            throw new InvalidOperationException("SQL bağlantısı bekleniyor.");

        await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await sql.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var count = await PriceSettingsSql.AdjustAsync(sql, factor, cancellationToken, transaction).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await InvalidateAsync().ConfigureAwait(false);
        return count;
    }

    private async Task InvalidateAsync()
    {
        cache.Remove(SnapshotCacheKey);
        await appCache.InvalidateSettingsAsync().ConfigureAwait(false);
        await appCache.InvalidateProductsAsync().ConfigureAwait(false);
    }
}
