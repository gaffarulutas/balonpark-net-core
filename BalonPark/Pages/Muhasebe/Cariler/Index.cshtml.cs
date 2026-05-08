using System.ComponentModel.DataAnnotations;
using BalonPark.Data;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Cariler;

public class IndexModel : BaseMuhasebePage
{
    private readonly CounterpartyRepository _counterpartyRepository;
    private readonly AccountMovementRepository _movementRepository;

    public IndexModel(
        AccountingCompanyRepository accountingCompanyRepository,
        CounterpartyRepository counterpartyRepository,
        AccountMovementRepository movementRepository)
        : base(accountingCompanyRepository)
    {
        _counterpartyRepository = counterpartyRepository;
        _movementRepository = movementRepository;
    }

    public List<(Counterparty C, decimal Debit, decimal Credit)> Rows { get; } = new();

    [BindProperty]
    public CariFormInput CreateInput { get; set; } = new();

    [BindProperty]
    public CariFormInput EditInput { get; set; } = new();

    [BindProperty]
    public int EditId { get; set; }

    /// <summary>GET ?yeni=true veya POST doğrulama hatasında yeni cari modalını aç.</summary>
    public bool OpenCreateModal { get; set; }

    public bool OpenEditModal { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Yeni { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Duzenle { get; set; }

    public class CariFormInput
    {
        [Required(ErrorMessage = "Unvan zorunludur.")]
        [StringLength(300, ErrorMessage = "Unvan en fazla 300 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cari tipi seçilmelidir.")]
        public CounterpartyType CounterpartyType { get; set; } = CounterpartyType.Both;

        [StringLength(20, ErrorMessage = "Vergi numarası en fazla 20 karakter olabilir.")]
        public string? TaxId { get; set; }

        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Telefon en fazla 30 karakter olabilir.")]
        public string? Phone { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadRowsAsync(cancellationToken).ConfigureAwait(false);
        var companyId = SelectedAccountingCompanyId!.Value;

        if (Duzenle is int did)
        {
            var entity = await _counterpartyRepository.GetByIdForCompanyAsync(did, companyId, cancellationToken).ConfigureAwait(false);
            if (entity != null)
            {
                EditId = did;
                EditInput = MapFromEntity(entity);
                OpenEditModal = true;
            }
        }
        else if (Yeni)
        {
            OpenCreateModal = true;
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken = default)
    {
        await LoadRowsAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenCreateModal = true;
            return Page();
        }

        var companyId = SelectedAccountingCompanyId!.Value;
        var entity = new Counterparty
        {
            CompanyId = companyId,
            Name = CreateInput.Name.Trim(),
            CounterpartyType = CreateInput.CounterpartyType,
            TaxId = string.IsNullOrWhiteSpace(CreateInput.TaxId) ? null : CreateInput.TaxId.Trim(),
            Email = string.IsNullOrWhiteSpace(CreateInput.Email) ? null : CreateInput.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(CreateInput.Phone) ? null : CreateInput.Phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(CreateInput.Notes) ? null : CreateInput.Notes.Trim(),
            IsActive = CreateInput.IsActive
        };

        await _counterpartyRepository.InsertAsync(entity, cancellationToken).ConfigureAwait(false);
        return RedirectToPage("/Muhasebe/Cariler/Index");
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken = default)
    {
        await LoadRowsAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenEditModal = true;
            return Page();
        }

        var companyId = SelectedAccountingCompanyId!.Value;
        var entity = await _counterpartyRepository.GetByIdForCompanyAsync(EditId, companyId, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return RedirectToPage("/Muhasebe/Cariler/Index");

        entity.Name = EditInput.Name.Trim();
        entity.CounterpartyType = EditInput.CounterpartyType;
        entity.TaxId = string.IsNullOrWhiteSpace(EditInput.TaxId) ? null : EditInput.TaxId.Trim();
        entity.Email = string.IsNullOrWhiteSpace(EditInput.Email) ? null : EditInput.Email.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(EditInput.Phone) ? null : EditInput.Phone.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(EditInput.Notes) ? null : EditInput.Notes.Trim();
        entity.IsActive = EditInput.IsActive;

        await _counterpartyRepository.UpdateAsync(entity, companyId, cancellationToken).ConfigureAwait(false);
        return RedirectToPage("/Muhasebe/Cariler/Index");
    }

    private static CariFormInput MapFromEntity(Counterparty entity) => new()
    {
        Name = entity.Name,
        CounterpartyType = entity.CounterpartyType,
        TaxId = entity.TaxId,
        Email = entity.Email,
        Phone = entity.Phone,
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };

    private async Task LoadRowsAsync(CancellationToken cancellationToken)
    {
        Rows.Clear();
        var companyId = SelectedAccountingCompanyId!.Value;
        var list = await _counterpartyRepository.GetByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
        foreach (var c in list)
        {
            var totals = await _movementRepository.GetTotalsForCounterpartyAsync(companyId, c.Id, cancellationToken).ConfigureAwait(false);
            Rows.Add((c, totals.DebitTotal, totals.CreditTotal));
        }
    }
}
