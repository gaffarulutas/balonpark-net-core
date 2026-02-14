using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using BalonPark.Data;
using BalonPark.Models;
using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Caching.Memory;

namespace BalonPark.Services.GoogleAnalytics;

/// <summary>
/// GA4 raporlarını anlık getirir. Veritabanına kaydetmez; memory cache ile API kotası korunur (best practice).
/// </summary>
public class GoogleAnalyticsService(
    IConfiguration configuration,
    ILogger<GoogleAnalyticsService> logger,
    SettingsRepository settingsRepository,
    IMemoryCache memoryCache) : IGoogleAnalyticsService
{
    private const string CacheKeyDashboard = "GA:Dashboard";
    private static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromMinutes(2);

    public async Task<GoogleAnalyticsDashboardDto> GetDashboardAsync(bool skipCache = false, CancellationToken cancellationToken = default)
    {
        if (!skipCache && memoryCache.TryGetValue(CacheKeyDashboard, out GoogleAnalyticsDashboardDto? cached))
            return cached!;

        var settings = await settingsRepository.GetFirstAsync();
        var propertyId = settings?.GoogleAnalyticsPropertyId?.Trim();
        if (string.IsNullOrEmpty(propertyId))
        {
            return new GoogleAnalyticsDashboardDto
            {
                Configured = false,
                ErrorMessage = "Google Analytics Property ID ayarlanmamış. Admin → Ayarlar → Google Analytics Property ID alanını doldurun."
            };
        }

        var propertyName = $"properties/{propertyId}";
        GoogleCredential? credential = null;

        try
        {
            var json = settings?.GoogleShoppingServiceAccountKeyJson;
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
#pragma warning disable CS0618 // Tür veya üye eski - Google.Apis.Auth uyarısı; proje GoogleShoppingService ile aynı deseni kullanıyor
                credential = GoogleCredential.FromStream(ms)
                    .CreateScoped("https://www.googleapis.com/auth/analytics.readonly");
#pragma warning restore CS0618
            }
            else
            {
                var keyPath = configuration["GoogleAnalytics:ServiceAccountKeyPath"] ?? "";
                var paths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), keyPath.Replace("~/", "")),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "balonpark.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Keys", "balonpark.json")
                };
                var credentialPath = paths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p));
                if (credentialPath == null)
                {
                    return new GoogleAnalyticsDashboardDto
                    {
                        Configured = true,
                        ErrorMessage = "Service account anahtarı bulunamadı. Ayarlarda JSON key girin veya Keys/ klasörüne koyun."
                    };
                }
#pragma warning disable CS0618
                credential = GoogleCredential.FromFile(credentialPath)
                    .CreateScoped("https://www.googleapis.com/auth/analytics.readonly");
#pragma warning restore CS0618
            }

            var client = new BetaAnalyticsDataClientBuilder { GoogleCredential = credential }.Build();
            var result = new GoogleAnalyticsDashboardDto { Configured = true, FetchedAt = DateTime.UtcNow };

            // Realtime: anlık aktif kullanıcı
            try
            {
                var realtimeRequest = new RunRealtimeReportRequest
                {
                    Property = propertyName,
                    Metrics = { new Metric { Name = "activeUsers" } }
                };
                var realtimeResponse = await client.RunRealtimeReportAsync(realtimeRequest, cancellationToken: cancellationToken);
                result.RealtimeActiveUsers = GetMetricValue(realtimeResponse, "activeUsers", 0);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GA anlık rapor alınamadı");
                if (IsApiDisabledOrPermissionDenied(ex))
                {
                    result.ErrorMessage = GetFriendlyApiErrorMessage(ex);
                    // Hata önbelleğe alınmaz; her istek/yenile tekrar API dener
                    return result;
                }
            }

            // Bugün, son 7 gün, son 30 gün (tek RunReport ile 3 date range)
            try
            {
                var today = DateTime.UtcNow.Date;
                var request = new RunReportRequest
                {
                    Property = propertyName,
                    DateRanges =
                    {
                        new DateRange { StartDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), EndDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                        new DateRange { StartDate = today.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), EndDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                        new DateRange { StartDate = today.AddDays(-29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), EndDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }
                    },
                    Dimensions = { new Dimension { Name = "date" } },
                    Metrics =
                    {
                        new Google.Analytics.Data.V1Beta.Metric { Name = "sessions" },
                        new Google.Analytics.Data.V1Beta.Metric { Name = "totalUsers" },
                        new Google.Analytics.Data.V1Beta.Metric { Name = "screenPageViews" },
                        new Google.Analytics.Data.V1Beta.Metric { Name = "bounceRate" },
                        new Google.Analytics.Data.V1Beta.Metric { Name = "engagementRate" }
                    }
                };
                var response = await client.RunReportAsync(request, cancellationToken: cancellationToken);
                if (response.Totals.Count >= 3)
                {
                    result.Today = RowToOverview(response.Totals[0], response.MetricHeaders);
                    result.Last7Days = RowToOverview(response.Totals[1], response.MetricHeaders);
                    result.Last30Days = RowToOverview(response.Totals[2], response.MetricHeaders);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GA özet rapor alınamadı");
                if (IsApiDisabledOrPermissionDenied(ex))
                {
                    result.ErrorMessage = GetFriendlyApiErrorMessage(ex);
                    return result;
                }
                if (result.ErrorMessage == null) result.ErrorMessage = ex.Message;
            }

            // Top sayfalar (son 30 gün)
            try
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var endDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var topRequest = new RunReportRequest
                {
                    Property = propertyName,
                    DateRanges = { new DateRange { StartDate = startDate, EndDate = endDate } },
                    Dimensions = { new Dimension { Name = "pagePath" }, new Dimension { Name = "pageTitle" } },
                    Metrics = { new Google.Analytics.Data.V1Beta.Metric { Name = "screenPageViews" } },
                    Limit = 10,
                    OrderBys = { new OrderBy { Metric = new OrderBy.Types.MetricOrderBy { MetricName = "screenPageViews" }, Desc = true } }
                };
                var topResponse = await client.RunReportAsync(topRequest, cancellationToken: cancellationToken);
                foreach (var row in topResponse.Rows)
                {
                    result.TopPages.Add(new GaPageRow
                    {
                        PagePath = GetDimensionValue(row, 0),
                        PageTitle = GetDimensionValue(row, 1),
                        Views = (long)GetMetricValueFromRow(row, "screenPageViews", topResponse.MetricHeaders)
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GA en çok görüntülenen sayfalar raporu alınamadı");
                if (IsApiDisabledOrPermissionDenied(ex))
                {
                    result.ErrorMessage = GetFriendlyApiErrorMessage(ex);
                    return result;
                }
            }

            // Trafik kaynakları (son 30 gün)
            try
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var endDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var sourceRequest = new RunReportRequest
                {
                    Property = propertyName,
                    DateRanges = { new DateRange { StartDate = startDate, EndDate = endDate } },
                    Dimensions = { new Dimension { Name = "sessionDefaultChannelGroup" } },
                    Metrics =
                    {
                        new Google.Analytics.Data.V1Beta.Metric { Name = "sessions" },
                        new Google.Analytics.Data.V1Beta.Metric { Name = "totalUsers" }
                    },
                    Limit = 10,
                    OrderBys = { new OrderBy { Metric = new OrderBy.Types.MetricOrderBy { MetricName = "sessions" }, Desc = true } }
                };
                var sourceResponse = await client.RunReportAsync(sourceRequest, cancellationToken: cancellationToken);
                foreach (var row in sourceResponse.Rows)
                {
                    result.TrafficSources.Add(new GaSourceRow
                    {
                        Channel = GetDimensionValue(row, 0),
                        Sessions = (long)GetMetricValueFromRow(row, "sessions", sourceResponse.MetricHeaders),
                        Users = (long)GetMetricValueFromRow(row, "totalUsers", sourceResponse.MetricHeaders)
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GA trafik kaynakları raporu alınamadı");
                if (IsApiDisabledOrPermissionDenied(ex))
                {
                    result.ErrorMessage = GetFriendlyApiErrorMessage(ex);
                    return result;
                }
            }

            memoryCache.Set(CacheKeyDashboard, result, DashboardCacheTtl);
        return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Analytics dashboard yüklenemedi");
            var friendlyMessage = GetFriendlyApiErrorMessage(ex);
            return new GoogleAnalyticsDashboardDto
            {
                Configured = true,
                ErrorMessage = friendlyMessage,
                FetchedAt = DateTime.UtcNow
            };
        }
    }

    private static bool IsApiDisabledOrPermissionDenied(Exception ex)
    {
        var text = ex.Message + (ex.InnerException?.Message ?? "");
        return text.Contains("PermissionDenied", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("has not been used", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("is disabled", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("analyticsdata.googleapis.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PermissionDenied hatalarında kullanıcıya anlamlı mesaj döner:
    /// - "bu property için yeterli yetki yok" → Service Account GA4'te Görüntüleyen olarak eklenmeli
    /// - "API kullanılmamış veya devre dışı" → GCP'de API etkinleştirilmeli
    /// </summary>
    private static string GetFriendlyApiErrorMessage(Exception ex)
    {
        var text = ex.Message + (ex.InnerException?.Message ?? "");
        if (!text.Contains("PermissionDenied", StringComparison.OrdinalIgnoreCase))
            return ex.Message;

        // "Bu property için yeterli yetki yok" → GA4 property'ye Service Account eklenmeli
        if (text.Contains("sufficient permissions", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("permissions for this property", StringComparison.OrdinalIgnoreCase))
        {
            return "Service Account bu GA4 property'ye erişemiyor. " +
                   "Google Analytics 4 → Admin → Property → Property Access Management (veya Property ayarları → Kullanıcı yönetimi) bölümüne gidin. " +
                   "Ayarlardaki Service Account e‑postasını (Google Shopping JSON içindeki client_email) \"Görüntüleyen\" (Viewer) rolüyle ekleyin. " +
                   "Kaydettikten sonra birkaç dakika bekleyip \"Yenile\" butonuna tıklayın.";
        }

        // GCP projesinde API etkin değil
        if (!text.Contains("analyticsdata", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("has not been used", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("is disabled", StringComparison.OrdinalIgnoreCase))
            return ex.Message;

        var match = Regex.Match(text, @"https://console\.developers\.google\.com/apis/api/analyticsdata\.googleapis\.com/overview\?project=\d+");
        var enableUrl = match.Success
            ? match.Value
            : "https://console.developers.google.com/apis/api/analyticsdata.googleapis.com/overview";

        return "Google Analytics Data API bu GCP projesinde etkin değil. " +
               "Etkinleştirmek için aşağıdaki bağlantıya gidip \"Etkinleştir\" (Enable) butonuna tıklayın. " +
               "Etkinleştirdikten sonra 2-5 dakika bekleyin, ardından bu sayfadaki \"Yenile\" butonuna tıklayın (tarayıcı F5 değil). " +
               "Link: " + enableUrl;
    }

    private static int GetMetricValue(RunRealtimeReportResponse response, string metricName, int defaultVal)
    {
        if (response.Totals.Count == 0) return defaultVal;
        var row = response.Totals[0];
        for (var i = 0; i < response.MetricHeaders.Count; i++)
        {
            if (string.Equals(response.MetricHeaders[i].Name, metricName, StringComparison.OrdinalIgnoreCase)
                && i < row.MetricValues.Count)
            {
                return int.TryParse(row.MetricValues[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultVal;
            }
        }
        return defaultVal;
    }

    private static double GetMetricValueFromRow(Row row, string metricName, IEnumerable<MetricHeader> headers)
    {
        var idx = 0;
        foreach (var h in headers)
        {
            if (string.Equals(h.Name, metricName, StringComparison.OrdinalIgnoreCase) && idx < row.MetricValues.Count)
                return double.TryParse(row.MetricValues[idx].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
            idx++;
        }
        return 0;
    }

    private static string GetDimensionValue(Row row, int index)
    {
        if (index < row.DimensionValues.Count)
            return row.DimensionValues[index].Value ?? "";
        return "";
    }

    private static GaOverviewRow? RowToOverview(Row row, IEnumerable<MetricHeader> headers)
    {
        var metricNames = headers.Select(h => h.Name ?? "").ToList();
        var values = row.MetricValues.Select(m => m.Value ?? "0").ToList();
        double GetM(string name)
        {
            var i = metricNames.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            if (i >= 0 && i < values.Count && double.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            return 0;
        }
        return new GaOverviewRow
        {
            Sessions = (long)GetM("sessions"),
            Users = (long)GetM("totalUsers"),
            ScreenPageViews = (long)GetM("screenPageViews"),
            BounceRate = GetM("bounceRate"),
            EngagementRate = GetM("engagementRate")
        };
    }
}
