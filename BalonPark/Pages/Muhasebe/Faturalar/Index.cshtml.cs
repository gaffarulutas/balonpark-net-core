using System.ComponentModel.DataAnnotations;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BalonPark.Pages.Muhasebe.Faturalar;

public class IndexModel : BaseMuhasebePage
{
    private readonly InvoiceRepository _invoiceRepository;
    private readonly CounterpartyRepository _counterpartyRepository;

    public IndexModel(
        AccountingCompanyRepository accountingCompanyRepository,
        InvoiceRepository invoiceRepository,
        CounterpartyRepository counterpartyRepository)
        : base(accountingCompanyRepository)
    {
        _invoiceRepository = invoiceRepository;
        _counterpartyRepository = counterpartyRepository;
    }

    public IReadOnlyList<Invoice> Invoices { get; private set; } = Array.Empty<Invoice>();

    public IReadOnlyList<Counterparty> Counterparties { get; private set; } = Array.Empty<Counterparty>();

    [BindProperty]
    public FaturaCreateInput CreateInvoiceInput { get; set; } = new();

    public bool OpenCreateModal { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Yeni { get; set; }

    public class FaturaCreateInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir cari seçin.")]
        public int CounterpartyId { get; set; }

        [Required(ErrorMessage = "Fatura yönü seçilmelidir.")]
        public InvoiceDirection Direction { get; set; } = InvoiceDirection.Outgoing;

        [Required(ErrorMessage = "Fatura numarası zorunludur.")]
        [StringLength(50, ErrorMessage = "Fatura numarası en fazla 50 karakter olabilir.")]
        public string InvoiceNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fatura tarihi zorunludur.")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        public DateTime? DueDate { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi üç harfli kod olmalıdır (örn. TRY).")]
        public string Currency { get; set; } = "TRY";

        [Range(0.0, 999999999999.0, ErrorMessage = "Matrah (KDV hariç) geçerli bir tutar olmalıdır.")]
        public decimal AmountNet { get; set; }

        [Range(0.0, 999999999999.0, ErrorMessage = "KDV tutarı geçerli bir değer olmalıdır.")]
        public decimal AmountVat { get; set; }

        [Range(0.01, 999999999999.0, ErrorMessage = "Brüt tutar sıfırdan büyük olmalıdır.")]
        public decimal AmountGross { get; set; }

        public string? Notes { get; set; }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        Invoices = await _invoiceRepository.GetByCompanyAsync(companyId, 500, cancellationToken).ConfigureAwait(false);
        Counterparties = await _counterpartyRepository.GetByCompanyAsync(companyId, cancellationToken).ConfigureAwait(false);
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (Yeni)
            OpenCreateModal = true;
    }

    public async Task<IActionResult> OnPostCreateInvoiceAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenCreateModal = true;
            return Page();
        }

        var companyId = SelectedAccountingCompanyId!.Value;
        var cp = await _counterpartyRepository.GetByIdForCompanyAsync(CreateInvoiceInput.CounterpartyId, companyId, cancellationToken).ConfigureAwait(false);
        if (cp == null || !cp.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Cari bulunamadı veya pasif.");
            OpenCreateModal = true;
            return Page();
        }

        if (!AccountingValidation.AreInvoiceAmountsConsistent(CreateInvoiceInput.AmountNet, CreateInvoiceInput.AmountVat, CreateInvoiceInput.AmountGross))
        {
            ModelState.AddModelError(string.Empty, $"Matrah + KDV, brüt tutar ile uyumlu olmalı (tolerans ±{AccountingValidation.InvoiceAmountTolerance:N2} TL).");
            OpenCreateModal = true;
            return Page();
        }

        var currency = (CreateInvoiceInput.Currency ?? "TRY").Trim().ToUpperInvariant();
        if (currency.Length != 3)
            currency = "TRY";

        var adminId = HttpContext.Session.GetAdminId() ?? 0;
        var invoice = new Invoice
        {
            CompanyId = companyId,
            CounterpartyId = CreateInvoiceInput.CounterpartyId,
            Direction = CreateInvoiceInput.Direction,
            InvoiceNo = CreateInvoiceInput.InvoiceNo.Trim(),
            IssueDate = CreateInvoiceInput.IssueDate.Date,
            DueDate = CreateInvoiceInput.DueDate?.Date,
            Currency = currency,
            AmountNet = CreateInvoiceInput.AmountNet,
            AmountVat = CreateInvoiceInput.AmountVat,
            AmountGross = CreateInvoiceInput.AmountGross,
            Notes = string.IsNullOrWhiteSpace(CreateInvoiceInput.Notes) ? null : CreateInvoiceInput.Notes.Trim()
        };

        try
        {
            await _invoiceRepository.CreateWithMovementAsync(invoice, adminId, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            OpenCreateModal = true;
            return Page();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            // 2601 = unique index, 2627 = unique constraint (aynı senaryo: çift fatura no)
            ModelState.AddModelError(string.Empty, "Bu fatura numarası bu şirket için zaten kayıtlı.");
            OpenCreateModal = true;
            return Page();
        }

        return RedirectToPage("/Muhasebe/Faturalar/Index");
    }
}
