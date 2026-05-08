namespace BalonPark.Models.Accounting;

public class AccountingCompany
{
    public int Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? TaxOffice { get; set; }
    public string DefaultCurrency { get; set; } = "TRY";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
