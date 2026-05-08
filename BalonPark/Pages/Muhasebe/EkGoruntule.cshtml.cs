using BalonPark.Data;
using BalonPark.Services.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace BalonPark.Pages.Muhasebe;

public class EkGoruntuleModel : BaseMuhasebePage
{
    private readonly InvoiceAttachmentRepository _attachmentRepository;
    private readonly IInvoiceBlobStorage _blobStorage;

    public EkGoruntuleModel(
        AccountingCompanyRepository accountingCompanyRepository,
        InvoiceAttachmentRepository attachmentRepository,
        IInvoiceBlobStorage blobStorage)
        : base(accountingCompanyRepository)
    {
        _attachmentRepository = attachmentRepository;
        _blobStorage = blobStorage;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        var att = await _attachmentRepository.GetByIdForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        if (att == null)
            return NotFound();

        var stream = await _blobStorage.OpenReadAsync(att.StorageKey, cancellationToken).ConfigureAwait(false);
        if (stream == null)
            return NotFound();

        var safeFileName = SanitizeFileName(att.OriginalFileName);
        var encodedFileName = Uri.EscapeDataString(safeFileName);
        Response.Headers[HeaderNames.ContentDisposition] = $"inline; filename*=UTF-8''{encodedFileName}";

        return File(stream, string.IsNullOrWhiteSpace(att.ContentType) ? "application/octet-stream" : att.ContentType);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "dosya";

        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            if (char.IsControl(ch) || ch is '/' or '\\' or '"')
                continue;

            builder.Append(ch);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "dosya" : sanitized;
    }
}
