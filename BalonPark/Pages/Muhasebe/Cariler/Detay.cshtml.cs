using System.ComponentModel.DataAnnotations;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Cariler;

public class DetayModel : BaseMuhasebePage
{
    private readonly CounterpartyRepository _counterpartyRepository;
    private readonly AccountMovementRepository _movementRepository;

    public DetayModel(
        AccountingCompanyRepository accountingCompanyRepository,
        CounterpartyRepository counterpartyRepository,
        AccountMovementRepository movementRepository)
        : base(accountingCompanyRepository)
    {
        _counterpartyRepository = counterpartyRepository;
        _movementRepository = movementRepository;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public OdemeInputModel Odeme { get; set; } = new();

    public Counterparty? Counterparty { get; set; }
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public IReadOnlyList<AccountMovement> Movements { get; private set; } = Array.Empty<AccountMovement>();

    [TempData]
    public string? OdemeMessage { get; set; }

    /// <summary>POST sonrası doğrulama hatasında tahsilat modalını açık göster.</summary>
    public bool OpenOdemeModal { get; set; }

    public class OdemeInputModel
    {
        [Required(ErrorMessage = "İşlem tarihi zorunludur.")]
        public DateTime MovementDate { get; set; } = DateTime.Today;

        [Range(0.01, 999999999999.0, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        /// <summary>tahsilat: tahsilat (borç tarafı), odeme: ödeme (alacak tarafı)</summary>
        [Required(ErrorMessage = "Tahsilat veya ödeme seçin.")]
        public string Kind { get; set; } = "tahsilat";

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        /// <summary>Hareket para birimi (örn. TRY, üç harf).</summary>
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi üç harfli kod olmalıdır (örn. TRY).")]
        public string Currency { get; set; } = "TRY";
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadDetayAsync(cancellationToken).ConfigureAwait(false);
        if (Counterparty == null)
            return RedirectToPage("/Muhasebe/Cariler/Index");
        Odeme.Currency = PopularIso4217Currencies.NormalizeSelected(CurrentAccountingCompany?.DefaultCurrency);
        return Page();
    }

    private async Task LoadDetayAsync(CancellationToken cancellationToken)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        Counterparty = await _counterpartyRepository.GetByIdForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        if (Counterparty != null)
        {
            var totals = await _movementRepository.GetTotalsForCounterpartyAsync(companyId, Id, cancellationToken).ConfigureAwait(false);
            DebitTotal = totals.DebitTotal;
            CreditTotal = totals.CreditTotal;
            Movements = await _movementRepository.GetByCompanyAsync(companyId, Id, 200, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IActionResult> OnPostOdemeAsync(CancellationToken cancellationToken = default)
    {
        await LoadDetayAsync(cancellationToken).ConfigureAwait(false);
        if (Counterparty == null || !Counterparty.IsActive)
            return RedirectToPage("/Muhasebe/Cariler/Index");

        if (!ModelState.IsValid)
        {
            OpenOdemeModal = true;
            return Page();
        }

        var kind = (Odeme.Kind ?? "").Trim().ToLowerInvariant();
        if (kind is not ("tahsilat" or "odeme"))
        {
            ModelState.AddModelError(string.Empty, "Geçersiz hareket türü.");
            OpenOdemeModal = true;
            return Page();
        }

        var isCredit = kind == "odeme";
        var cur = PopularIso4217Currencies.NormalizeSelected(Odeme.Currency);
        if (!PopularIso4217Currencies.IsInList(cur))
        {
            cur = PopularIso4217Currencies.NormalizeSelected(CurrentAccountingCompany?.DefaultCurrency);
            if (!PopularIso4217Currencies.IsInList(cur))
                cur = "TRY";
        }

        var desc = string.IsNullOrWhiteSpace(Odeme.Description)
            ? (kind == "tahsilat" ? "Tahsilat" : "Ödeme")
            : Odeme.Description.Trim();

        var movement = new AccountMovement
        {
            CompanyId = SelectedAccountingCompanyId!.Value,
            CounterpartyId = Id,
            MovementDate = Odeme.MovementDate.Date,
            IsCredit = isCredit,
            Amount = Odeme.Amount,
            Currency = cur,
            ReferenceType = AccountingConstants.ReferenceTypePayment,
            ReferenceId = null,
            Description = desc,
            CreatedByAdminId = HttpContext.Session.GetAdminId()
        };

        await _movementRepository.InsertAsync(movement, cancellationToken).ConfigureAwait(false);
        OdemeMessage = "Ödeme veya tahsilat kaydı oluşturuldu.";
        return RedirectToPage(new { Id });
    }
}
