namespace BalonPark.Models.Accounting;

public class InvoiceAttachment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Sha256Hex { get; set; }
    public int? UploadedByAdminId { get; set; }
    public DateTime UploadedAt { get; set; }

    public int CompanyId { get; set; }
}
