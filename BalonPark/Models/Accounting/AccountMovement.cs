namespace BalonPark.Models.Accounting;

public class AccountMovement
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int CounterpartyId { get; set; }
    public DateTime MovementDate { get; set; }
    /// <summary>True: alacak, False: borç (cari hesap kolonları).</summary>
    public bool IsCredit { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string ReferenceType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public int? CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? CounterpartyName { get; set; }
}
