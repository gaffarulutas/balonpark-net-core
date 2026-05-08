using BalonPark.Data;
using BalonPark.Services.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace BalonPark.Pages.Muhasebe;

public class EkIndirModel : BaseMuhasebePage
{
    private readonly InvoiceAttachmentRepository _attachmentRepository;
    private readonly IInvoiceBlobStorage _blobStorage;

    public EkIndirModel(
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

        return File(stream, att.ContentType, att.OriginalFileName);
    }
}
