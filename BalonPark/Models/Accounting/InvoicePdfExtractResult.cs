namespace BalonPark.Models.Accounting;

/// <summary>Turkish e-fatura/e-arşiv PDF'inden çıkarılan fatura verilerini taşır.</summary>
public sealed class InvoicePdfExtractResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Fatura bilgileri ──────────────────────────────────────────
    public string? InvoiceNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal AmountVat { get; set; }
    public decimal AmountGross { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }

    // ── Karşı taraf (yöne göre: alınan = satıcı, kesilen = alıcı) ──
    public string? CounterpartyName { get; set; }
    public string? CounterpartyTaxId { get; set; }
    public string? CounterpartyTaxOffice { get; set; }
    public string? CounterpartyEmail { get; set; }
    public string? CounterpartyPhone { get; set; }
}
