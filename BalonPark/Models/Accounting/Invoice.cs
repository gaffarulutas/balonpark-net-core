namespace BalonPark.Models.Accounting;

public class Invoice
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int CounterpartyId { get; set; }
    public InvoiceDirection Direction { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal AmountNet { get; set; }
    public decimal AmountVat { get; set; }
    public decimal AmountGross { get; set; }
    public bool IsCancelled { get; set; }
    public string? Notes { get; set; }
    public int? CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Cari unvanı (liste için JOIN sonucu).</summary>
    public string? CounterpartyName { get; set; }
}
