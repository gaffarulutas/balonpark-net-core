using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using BalonPark.Services.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BalonPark.Pages.Muhasebe.Faturalar;

public class IndexModel : BaseMuhasebePage
{
    private readonly InvoiceRepository _invoiceRepository;
    private readonly CounterpartyRepository _counterpartyRepository;
    private readonly InvoicePdfParserService _pdfParser;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        AccountingCompanyRepository accountingCompanyRepository,
        InvoiceRepository invoiceRepository,
        CounterpartyRepository counterpartyRepository,
        InvoicePdfParserService pdfParser,
        ILogger<IndexModel> logger)
        : base(accountingCompanyRepository)
    {
        _invoiceRepository = invoiceRepository;
        _counterpartyRepository = counterpartyRepository;
        _pdfParser = pdfParser;
        _logger = logger;
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

        public decimal AmountGross { get; set; }

        public string? Notes { get; set; }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        Invoices = await _invoiceRepository.GetByCompanyAsync(companyId, 500, ct).ConfigureAwait(false);
        Counterparties = await _counterpartyRepository.GetByCompanyAsync(companyId, ct).ConfigureAwait(false);
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (Yeni) OpenCreateModal = true;
    }

    // ── PDF çıkarma (AJAX) ────────────────────────────────────────────────────

    /// <summary>
    /// AJAX endpoint: PDF faturayı okur, karşı tarafı bulur/oluşturur ve JSON döner.
    /// Yön (Incoming/Outgoing) PDF'deki hangi tarafın cari olduğunu belirler.
    /// </summary>
    public async Task<IActionResult> OnPostExtractPdfAsync(
        IFormFile? pdfFile,
        string? direction,
        CancellationToken cancellationToken = default)
    {
        if (pdfFile is null || pdfFile.Length == 0)
            return Json(new { success = false, error = "Lütfen bir PDF dosyası seçin." });

        if (!IsPdfContentType(pdfFile))
            return Json(new { success = false, error = "Yalnızca PDF dosyası yüklenebilir." });

        if (pdfFile.Length > 10 * 1024 * 1024)
            return Json(new { success = false, error = "Dosya boyutu 10 MB'ı aşamaz." });

        if (SelectedAccountingCompanyId is null)
            return Json(new { success = false, error = "Önce bir muhasebe şirketi seçin." });

        var dir = direction == "Incoming" ? InvoiceDirection.Incoming : InvoiceDirection.Outgoing;

        // ── Metin çıkar ───────────────────────────────────────────────────────
        string pdfText;
        try
        {
            await using var stream = pdfFile.OpenReadStream();
            pdfText = _pdfParser.ExtractText(stream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF metin çıkarma hatası: {FileName}", pdfFile.FileName);
            return Json(new { success = false, error = "PDF okunamadı. Dosyanın geçerli bir e-fatura olduğundan emin olun." });
        }

        // ── Parse ─────────────────────────────────────────────────────────────
        var parsed = _pdfParser.Parse(pdfText, dir);

        // ── Cari bul veya oluştur ─────────────────────────────────────────────
        var companyId = SelectedAccountingCompanyId.Value;
        int counterpartyId = 0;
        bool isNew = false;
        string counterpartyStatus = string.Empty;

        if (!string.IsNullOrWhiteSpace(parsed.CounterpartyTaxId))
        {
            var existing = await _counterpartyRepository
                .GetByTaxIdAsync(parsed.CounterpartyTaxId, companyId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                counterpartyId = existing.Id;
                counterpartyStatus = "found";
            }
            else
            {
                var name = !string.IsNullOrWhiteSpace(parsed.CounterpartyName)
                    ? parsed.CounterpartyName.Trim()
                    : $"Cari ({parsed.CounterpartyTaxId})";

                var notes = BuildCounterpartyNotes(parsed.CounterpartyTaxOffice);
                var newCp = new Counterparty
                {
                    CompanyId = companyId,
                    Name = name,
                    TaxId = parsed.CounterpartyTaxId,
                    Email = parsed.CounterpartyEmail,
                    Phone = Truncate(parsed.CounterpartyPhone, 30),
                    Notes = notes,
                    CounterpartyType = CounterpartyType.Both,
                    IsActive = true
                };

                counterpartyId = await _counterpartyRepository
                    .InsertAsync(newCp, cancellationToken)
                    .ConfigureAwait(false);
                isNew = true;
                counterpartyStatus = "created";
            }
        }

        return Json(new
        {
            success = true,
            counterpartyId,
            counterpartyIsNew = isNew,
            counterpartyStatus,
            counterpartyName = parsed.CounterpartyName,
            counterpartyTaxId = parsed.CounterpartyTaxId,
            counterpartyTaxOffice = parsed.CounterpartyTaxOffice,
            invoiceNo = parsed.InvoiceNo,
            issueDate = parsed.IssueDate?.ToString("yyyy-MM-dd"),
            amountNet = parsed.AmountNet.ToString("F2", CultureInfo.InvariantCulture),
            amountVat = parsed.AmountVat.ToString("F2", CultureInfo.InvariantCulture),
            amountGross = parsed.AmountGross.ToString("F2", CultureInfo.InvariantCulture),
            currency = parsed.Currency
        });
    }

    // ── Fatura kaydet ─────────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostCreateInvoiceAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            OpenCreateModal = true;
            return Page();
        }

        var companyId = SelectedAccountingCompanyId!.Value;
        var cp = await _counterpartyRepository
            .GetByIdForCompanyAsync(CreateInvoiceInput.CounterpartyId, companyId, cancellationToken)
            .ConfigureAwait(false);

        if (cp is null || !cp.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Cari bulunamadı veya pasif.");
            OpenCreateModal = true;
            return Page();
        }

        // Brüt tutarı server-side hesapla (JS/format farkı kaynaklı yuvarlama sorunlarını önler)
        var computedGross = CreateInvoiceInput.AmountNet + CreateInvoiceInput.AmountVat;
        if (computedGross <= 0)
        {
            ModelState.AddModelError(string.Empty, "Matrah + KDV sıfırdan büyük olmalıdır.");
            OpenCreateModal = true;
            return Page();
        }

        var currency = (CreateInvoiceInput.Currency ?? "TRY").Trim().ToUpperInvariant();
        if (currency.Length != 3) currency = "TRY";

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
            AmountGross = computedGross,
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
            ModelState.AddModelError(string.Empty, "Bu fatura numarası bu şirket için zaten kayıtlı.");
            OpenCreateModal = true;
            return Page();
        }

        return RedirectToPage("/Muhasebe/Faturalar/Index");
    }

    // ── Yardımcı ──────────────────────────────────────────────────────────────

    private static IActionResult Json(object data) => new JsonResult(data);

    private static bool IsPdfContentType(IFormFile file)
    {
        var ct = file.ContentType ?? string.Empty;
        var name = file.FileName ?? string.Empty;
        return ct.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string? BuildCounterpartyNotes(string? taxOffice)
    {
        if (string.IsNullOrWhiteSpace(taxOffice)) return null;
        return $"Vergi Dairesi: {taxOffice.Trim()}";
    }

    private static string? Truncate(string? s, int max)
        => s is null ? null : (s.Length <= max ? s : s[..max]);
}
