namespace BalonPark.Services.CatalogSync;

public class CatalogSyncOptions
{
    public const string SectionName = "CatalogSync";

    public bool Enabled { get; set; }

    public List<CatalogSyncTargetOptions> Targets { get; set; } = [];
}

public class CatalogSyncTargetOptions
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Locale code used for AI prompts: el or de.</summary>
    public string Locale { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public string FtpHost { get; set; } = string.Empty;
    public int FtpPort { get; set; } = 21;
    public string FtpUser { get; set; } = string.Empty;
    public string FtpPass { get; set; } = string.Empty;

    /// <summary>Remote root that contains uploads/, e.g. /httpdocs/wwwroot</summary>
    public string FtpUploadsRoot { get; set; } = "/httpdocs/wwwroot";
}
