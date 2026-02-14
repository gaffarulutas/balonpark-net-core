using BalonPark.Services.GoogleAnalytics;

namespace BalonPark.Services;

/// <summary>
/// Google Analytics 4 raporlarını anlık getirir (veritabanına kaydetmez).
/// Bellek önbelleği ile API kotası korunur; kısa TTL ile güncel veri hissi sağlanır.
/// </summary>
public interface IGoogleAnalyticsService
{
    /// <summary>
    /// Dashboard için tüm raporları tek seferde getirir.
    /// Önbellek süresi: 2 dakika. Veritabanına yazılmaz. skipCache=true ile önbellek atlanır (Yenile butonu).
    /// </summary>
    Task<GoogleAnalyticsDashboardDto> GetDashboardAsync(bool skipCache = false, CancellationToken cancellationToken = default);
}
