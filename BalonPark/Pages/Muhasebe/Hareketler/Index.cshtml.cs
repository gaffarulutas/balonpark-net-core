using System.ComponentModel.DataAnnotations;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Hareketler;

public class IndexModel : BaseMuhasebePage
{
    private readonly AccountMovementRepository _movementRepository;
    private readonly CounterpartyRepository _counterpartyRepository;

    public IndexModel(
        AccountingCompanyRepository accountingCompanyRepository,
        AccountMovementRepository movementRepository,
        CounterpartyRepository counterpartyRepository)
        : base(accountingCompanyRepository)
    {
        _movementRepository = movementRepository;
        _counterpartyRepository = counterpartyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int? CariId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Manuel { get; set; }

    public bool OpenManuelModal { get; set; }

    public IReadOnlyList<AccountMovement> Movements { get; private set; } = Array.Empty<AccountMovement>();
    public IReadOnlyList<Counterparty> Counterparties { get; private set; } = Array.Empty<Counterparty>();

    [BindProperty]
    public ManualInputModel ManualInput { get; set; } = new();

    public class ManualInputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir cari seçin.")]
        public int CounterpartyId { get; set; }

        [Required(ErrorMessage = "Hareket tarihi zorunludur.")]
        public DateTime MovementDate { get; set; } = DateTime.Today;

        [Range(0.01, 999999999999.0, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        /// <summary>credit = alacak, debit = borç</summary>
        [Required(ErrorMessage = "Borç veya alacak tarafını seçin.")]
        public string Side { get; set; } = "credit";

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        /// <summary>Para birimi (örn. TRY). Boşsa şirket varsayılanı kullanılır.</summary>
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi üç harfli kod olmalıdır (örn. TRY).")]
        public string Currency { get; set; } = "TRY";
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        Counterparties = await _counterpartyRepository.GetByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
        Movements = await _movementRepository.GetByCompanyAsync(companyId, CariId, 300, cancellationToken).ConfigureAwait(false);
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (Manuel)
            OpenManuelModal = true;
    }

    public async Task<IActionResult> OnPostManuelAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenManuelModal = true;
            return Page();
        }

        var companyId = SelectedAccountingCompanyId!.Value;
        var cp = await _counterpartyRepository.GetByIdForCompanyAsync(ManualInput.CounterpartyId, companyId, cancellationToken).ConfigureAwait(false);
        if (cp == null || !cp.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Cari bulunamadı veya pasif.");
            OpenManuelModal = true;
            return Page();
        }

        var isCredit = string.Equals(ManualInput.Side, "credit", StringComparison.OrdinalIgnoreCase);
        var adminId = HttpContext.Session.GetAdminId();
        var cur = (ManualInput.Currency ?? "TRY").Trim().ToUpperInvariant();
        if (cur.Length != 3)
            cur = CurrentAccountingCompany?.DefaultCurrency?.Trim().Length >= 3
                ? CurrentAccountingCompany.DefaultCurrency.Trim().Substring(0, 3).ToUpperInvariant()
                : "TRY";

        var movement = new AccountMovement
        {
            CompanyId = companyId,
            CounterpartyId = ManualInput.CounterpartyId,
            MovementDate = ManualInput.MovementDate.Date,
            IsCredit = isCredit,
            Amount = ManualInput.Amount,
            Currency = cur,
            ReferenceType = AccountingConstants.ReferenceTypeManual,
            ReferenceId = null,
            Description = string.IsNullOrWhiteSpace(ManualInput.Description) ? "Manuel hareket" : ManualInput.Description.Trim(),
            CreatedByAdminId = adminId
        };

        await _movementRepository.InsertAsync(movement, cancellationToken).ConfigureAwait(false);
        return RedirectToPage(new { CariId });
    }
}
