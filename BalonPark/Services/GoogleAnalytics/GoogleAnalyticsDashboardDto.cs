namespace BalonPark.Services.GoogleAnalytics;

/// <summary>
/// Dashboard için tek seferde dönen GA4 raporları (veritabanına kaydedilmez, anlık API + memory cache).
/// </summary>
public class GoogleAnalyticsDashboardDto
{
    public bool Configured { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? FetchedAt { get; set; }

    /// <summary>Anlık sitede olan kullanıcı sayısı.</summary>
    public int RealtimeActiveUsers { get; set; }

    /// <summary>Son 7 gün: oturum, kullanıcı, sayfa görüntüleme.</summary>
    public GaOverviewRow? Last7Days { get; set; }

    /// <summary>Son 30 gün: oturum, kullanıcı, sayfa görüntüleme.</summary>
    public GaOverviewRow? Last30Days { get; set; }

    /// <summary>Bugün (0:00'dan itibaren): oturum, kullanıcı, sayfa görüntüleme.</summary>
    public GaOverviewRow? Today { get; set; }

    /// <summary>En çok görüntülenen sayfalar (son 30 gün).</summary>
    public List<GaPageRow> TopPages { get; set; } = new();

    /// <summary>Trafik kaynakları (son 30 gün).</summary>
    public List<GaSourceRow> TrafficSources { get; set; } = new();
}

public class GaOverviewRow
{
    public long Sessions { get; set; }
    public long Users { get; set; }
    public long ScreenPageViews { get; set; }
    public double BounceRate { get; set; }
    public double EngagementRate { get; set; }
}

public class GaPageRow
{
    public string PagePath { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public long Views { get; set; }
}

public class GaSourceRow
{
    public string Channel { get; set; } = string.Empty;
    public long Sessions { get; set; }
    public long Users { get; set; }
}
