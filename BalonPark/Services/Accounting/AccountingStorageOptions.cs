namespace BalonPark.Services.Accounting;

public class AccountingStorageOptions
{
    public const string SectionName = "Accounting";

    /// <summary>Storage provider: FileSystem or Ftp.</summary>
    public string StorageProvider { get; set; } = "FileSystem";

    /// <summary>Dosya sistemi depolama kökü (web köküne göre, başında / olmadan). Örn: uploads/accounting</summary>
    public string LocalRootRelativeToWebRoot { get; set; } = "uploads/accounting";

    public long MaxAttachmentSizeBytes { get; set; } = 20 * 1024 * 1024;

    public string? FtpHost { get; set; }

    public int FtpPort { get; set; } = 21;

    public string? FtpUsername { get; set; }

    public string? FtpPassword { get; set; }

    /// <summary>Fatura ekleri için FTP üzerinde kullanılacak kök dizin. Örn: /Sifre Koruma/fatura-ekleri</summary>
    public string FtpRootPath { get; set; } = "/Sifre Koruma/fatura-ekleri";

    /// <summary>FTP bağlantısında TLS kullanımı.</summary>
    public bool FtpUseSsl { get; set; }

    /// <summary>Geliştirme ortamında self-signed sertifika kabul etmek için true yapılabilir.</summary>
    public bool FtpAcceptAnyCertificate { get; set; }
}
