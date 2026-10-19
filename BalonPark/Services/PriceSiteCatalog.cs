using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Services;

public sealed record PriceSiteStatus(string Key, string Label, bool ShowPrices, bool Reachable, string? Error);

public interface IPriceSiteCatalog
{
    Task<IReadOnlyList<PriceSiteStatus>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<string> SaveFlagsAsync(IReadOnlyDictionary<string, bool> showPricesBySite, CancellationToken cancellationToken = default);
    Task<string> AdjustAsync(IReadOnlyCollection<string> siteKeys, int percent, bool increase, CancellationToken cancellationToken = default);
}

/// <summary>
/// Balonpark yönetiminden üç sitenin fiyat görünürlüğünü ve taban fiyatını günceller.
/// GR ve DE bağlantıları CatalogSync hedeflerinden okunur.
/// </summary>
public sealed class PriceSiteCatalog(
    IConfiguration configuration,
    IOptions<CatalogSyncOptions> catalogSync,
    IPriceSettingsStore localStore,
    ILogger<PriceSiteCatalog> logger) : IPriceSiteCatalog
{
    public async Task<IReadOnlyList<PriceSiteStatus>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var sites = Endpoints();
        var statuses = await Task.WhenAll(sites.Select(site => ReadStatusAsync(site, cancellationToken))).ConfigureAwait(false);
        return statuses;
    }

    public async Task<string> SaveFlagsAsync(IReadOnlyDictionary<string, bool> showPricesBySite, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        foreach (var site in Endpoints())
        {
            if (!showPricesBySite.TryGetValue(site.Key, out var show))
                continue;

            try
            {
                if (site.Local)
                    await localStore.SetShowPricesAsync(show, cancellationToken).ConfigureAwait(false);
                else
                    await WithRemoteAsync(site, async (connection, ct) =>
                    {
                        await PriceSettingsSql.SetFlagAsync(connection, show, ct).ConfigureAwait(false);
                        return 0;
                    }, cancellationToken).ConfigureAwait(false);

                lines.Add($"{site.Label}: fiyatlar {(show ? "açık" : "kapalı")}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fiyat görünürlüğü kaydedilemedi: {Site}", site.Key);
                lines.Add($"{site.Label}: kaydedilemedi");
            }
        }

        return lines.Count == 0 ? "Kaydedilecek site seçilmedi." : string.Join(" · ", lines);
    }

    public async Task<string> AdjustAsync(IReadOnlyCollection<string> siteKeys, int percent, bool increase, CancellationToken cancellationToken = default)
    {
        var selected = new HashSet<string>(siteKeys, StringComparer.OrdinalIgnoreCase);
        var factor = Helpers.PriceAdjustment.Factor(percent, increase);
        var verb = increase ? "yükseltildi" : "düşürüldü";
        var lines = new List<string>();

        foreach (var site in Endpoints())
        {
            if (!selected.Contains(site.Key))
                continue;

            try
            {
                int count;
                if (site.Local)
                    count = await localStore.AdjustPricesAsync(percent, increase, cancellationToken).ConfigureAwait(false);
                else
                    count = await WithRemoteAsync(site, async (connection, ct) =>
                    {
                        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
                        var updated = await PriceSettingsSql.AdjustAsync(connection, factor, ct, transaction).ConfigureAwait(false);
                        await transaction.CommitAsync(ct).ConfigureAwait(false);
                        return updated;
                    }, cancellationToken).ConfigureAwait(false);

                lines.Add($"{site.Label}: {count} ürün %{percent} {verb}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Toplu fiyat güncellenemedi: {Site}", site.Key);
                lines.Add($"{site.Label}: güncellenemedi");
            }
        }

        return lines.Count == 0
            ? "Güncellenecek site seçilmedi."
            : string.Join(" · ", lines) + ". Kuruş bırakılmadı.";
    }

    private async Task<PriceSiteStatus> ReadStatusAsync(SiteEndpoint site, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(site.ConnectionString))
            return new PriceSiteStatus(site.Key, site.Label, true, false, "Bağlantı yok");

        try
        {
            PriceSettingsSnapshot snapshot;
            if (site.Local)
                snapshot = await localStore.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            else
                snapshot = await WithRemoteAsync(site, PriceSettingsSql.ReadAsync, cancellationToken).ConfigureAwait(false);

            return new PriceSiteStatus(site.Key, site.Label, snapshot.ShowPrices, true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fiyat ayarı okunamadı: {Site}", site.Key);
            return new PriceSiteStatus(site.Key, site.Label, true, false, "Bağlantı kurulamadı");
        }
    }

    private static async Task<T> WithRemoteAsync<T>(SiteEndpoint site, Func<SqlConnection, CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(PriceSettingsSql.WithConnectTimeout(site.ConnectionString));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await action(connection, cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyList<SiteEndpoint> Endpoints()
    {
        var sites = new List<SiteEndpoint>
        {
            new("tr", "Balonpark", configuration.GetConnectionString("DefaultConnection") ?? string.Empty, true)
        };

        foreach (var target in catalogSync.Value.Targets)
        {
            var key = KeyFor(target);
            if (sites.Any(site => site.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                continue;

            sites.Add(new SiteEndpoint(key, LabelFor(key, target.Name), target.ConnectionString, false));
        }

        return sites;
    }

    private static string KeyFor(CatalogSyncTargetOptions target)
    {
        if (target.Locale.Equals("el", StringComparison.OrdinalIgnoreCase)
            || target.Name.Contains("gr", StringComparison.OrdinalIgnoreCase))
            return "gr";

        if (target.Locale.Equals("de", StringComparison.OrdinalIgnoreCase)
            || target.Name.Contains(".de", StringComparison.OrdinalIgnoreCase))
            return "de";

        var name = target.Name.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(name) ? "site" : name;
    }

    private static string LabelFor(string key, string fallback)
        => key switch
        {
            "gr" => "balapark.gr",
            "de" => "balapark.de",
            _ => string.IsNullOrWhiteSpace(fallback) ? key : fallback
        };

    private sealed record SiteEndpoint(string Key, string Label, string ConnectionString, bool Local);
}
