using System.ComponentModel.DataAnnotations;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe.Sirketler;

public class IndexModel : BaseMuhasebePage
{
    private readonly AccountingCompanyRepository _repository;

    public IndexModel(AccountingCompanyRepository accountingCompanyRepository)
        : base(accountingCompanyRepository)
    {
        _repository = accountingCompanyRepository;
    }

    protected override bool RequiresSelectedCompany => false;

    public IReadOnlyList<AccountingCompany> Companies { get; private set; } = Array.Empty<AccountingCompany>();

    [BindProperty]
    public SirketFormInput CreateCompanyInput { get; set; } = new();

    [BindProperty]
    public SirketFormInput EditCompanyInput { get; set; } = new();

    [BindProperty]
    public int EditCompanyId { get; set; }

    public bool OpenCreateModal { get; set; }

    public bool OpenEditModal { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Yeni { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Duzenle { get; set; }

    public class SirketFormInput
    {
        [Required(ErrorMessage = "Unvan zorunludur.")]
        [StringLength(300, ErrorMessage = "Unvan en fazla 300 karakter olabilir.")]
        public string LegalName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Vergi numarası en fazla 20 karakter olabilir.")]
        public string? TaxId { get; set; }

        [StringLength(150, ErrorMessage = "Vergi dairesi en fazla 150 karakter olabilir.")]
        public string? TaxOffice { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Varsayılan para birimi üç harfli kod olmalıdır (örn. TRY).")]
        public string DefaultCurrency { get; set; } = "TRY";

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string? Address { get; set; }

        [StringLength(30, ErrorMessage = "Telefon en fazla 30 karakter olabilir.")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        Companies = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (Duzenle is int eid)
        {
            var entity = await _repository.GetByIdAsync(eid, cancellationToken).ConfigureAwait(false);
            if (entity != null)
            {
                EditCompanyId = eid;
                EditCompanyInput = MapFromEntity(entity);
                OpenEditModal = true;
            }
        }
        else if (Yeni)
        {
            OpenCreateModal = true;
        }
    }

    public async Task<IActionResult> OnPostCreateCompanyAsync(CancellationToken cancellationToken = default)
    {
        Companies = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenCreateModal = true;
            return Page();
        }

        var currency = (CreateCompanyInput.DefaultCurrency ?? "TRY").Trim().ToUpperInvariant();
        if (currency.Length != 3)
            currency = "TRY";

        var entity = new AccountingCompany
        {
            LegalName = CreateCompanyInput.LegalName.Trim(),
            TaxId = string.IsNullOrWhiteSpace(CreateCompanyInput.TaxId) ? null : CreateCompanyInput.TaxId.Trim(),
            TaxOffice = string.IsNullOrWhiteSpace(CreateCompanyInput.TaxOffice) ? null : CreateCompanyInput.TaxOffice.Trim(),
            DefaultCurrency = currency,
            Address = string.IsNullOrWhiteSpace(CreateCompanyInput.Address) ? null : CreateCompanyInput.Address.Trim(),
            Phone = string.IsNullOrWhiteSpace(CreateCompanyInput.Phone) ? null : CreateCompanyInput.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(CreateCompanyInput.Email) ? null : CreateCompanyInput.Email.Trim(),
            IsActive = CreateCompanyInput.IsActive
        };

        var newId = await _repository.InsertAsync(entity, cancellationToken).ConfigureAwait(false);
        HttpContext.Session.SetSelectedAccountingCompanyId(newId);
        return RedirectToPage("/Muhasebe/Index");
    }

    public async Task<IActionResult> OnPostEditCompanyAsync(CancellationToken cancellationToken = default)
    {
        Companies = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenEditModal = true;
            return Page();
        }

        var existing = await _repository.GetByIdAsync(EditCompanyId, cancellationToken).ConfigureAwait(false);
        if (existing == null)
            return RedirectToPage("/Muhasebe/Sirketler/Index");

        var currency = (EditCompanyInput.DefaultCurrency ?? "TRY").Trim().ToUpperInvariant();
        if (currency.Length != 3)
            currency = "TRY";

        existing.LegalName = EditCompanyInput.LegalName.Trim();
        existing.TaxId = string.IsNullOrWhiteSpace(EditCompanyInput.TaxId) ? null : EditCompanyInput.TaxId.Trim();
        existing.TaxOffice = string.IsNullOrWhiteSpace(EditCompanyInput.TaxOffice) ? null : EditCompanyInput.TaxOffice.Trim();
        existing.DefaultCurrency = currency;
        existing.Address = string.IsNullOrWhiteSpace(EditCompanyInput.Address) ? null : EditCompanyInput.Address.Trim();
        existing.Phone = string.IsNullOrWhiteSpace(EditCompanyInput.Phone) ? null : EditCompanyInput.Phone.Trim();
        existing.Email = string.IsNullOrWhiteSpace(EditCompanyInput.Email) ? null : EditCompanyInput.Email.Trim();
        existing.IsActive = EditCompanyInput.IsActive;

        await _repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return RedirectToPage("/Muhasebe/Sirketler/Index");
    }

    private static SirketFormInput MapFromEntity(AccountingCompany entity) => new()
    {
        LegalName = entity.LegalName,
        TaxId = entity.TaxId,
        TaxOffice = entity.TaxOffice,
        DefaultCurrency = entity.DefaultCurrency.Trim(),
        Address = entity.Address,
        Phone = entity.Phone,
        Email = entity.Email,
        IsActive = entity.IsActive
    };
}
