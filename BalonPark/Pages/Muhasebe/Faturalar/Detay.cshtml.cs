using System.Security.Cryptography;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models.Accounting;
using BalonPark.Services.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BalonPark.Pages.Muhasebe.Faturalar;

public class DetayModel : BaseMuhasebePage
{
    private readonly InvoiceRepository _invoiceRepository;
    private readonly InvoiceAttachmentRepository _attachmentRepository;
    private readonly IInvoiceBlobStorage _blobStorage;
    private readonly AccountingStorageOptions _storageOptions;
    private readonly ILogger<DetayModel> _logger;

    public DetayModel(
        AccountingCompanyRepository accountingCompanyRepository,
        InvoiceRepository invoiceRepository,
        InvoiceAttachmentRepository attachmentRepository,
        IInvoiceBlobStorage blobStorage,
        IOptions<AccountingStorageOptions> storageOptions,
        ILogger<DetayModel> logger)
        : base(accountingCompanyRepository)
    {
        _invoiceRepository = invoiceRepository;
        _attachmentRepository = attachmentRepository;
        _blobStorage = blobStorage;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Invoice? Invoice { get; set; }
    public IReadOnlyList<InvoiceAttachment> Attachments { get; private set; } = Array.Empty<InvoiceAttachment>();

    [TempData]
    public string? UploadMessage { get; set; }

    [TempData]
    public string? ActionMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        Invoice = await _invoiceRepository.GetByIdForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        if (Invoice == null)
            return RedirectToPage("/Muhasebe/Faturalar/Index");

        Attachments = await _attachmentRepository.GetByInvoiceForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostIptalAsync(CancellationToken cancellationToken = default)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        var invoice = await _invoiceRepository.GetByIdForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        if (invoice == null)
            return RedirectToPage("/Muhasebe/Faturalar/Index");

        if (invoice.IsCancelled)
        {
            ActionMessage = "Fatura zaten iptal edilmiş.";
            return RedirectToPage(new { Id });
        }

        var adminId = HttpContext.Session.GetAdminId() ?? 0;
        var ok = await _invoiceRepository.CancelInvoiceAsync(Id, companyId, adminId, cancellationToken).ConfigureAwait(false);
        ActionMessage = ok ? "Fatura iptal edildi; cariye ters hareket yazıldı." : "İptal işlemi yapılamadı.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostUploadAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        var companyId = SelectedAccountingCompanyId!.Value;
        var invoice = await _invoiceRepository.GetByIdForCompanyAsync(Id, companyId, cancellationToken).ConfigureAwait(false);
        if (invoice == null)
            return RedirectToPage("/Muhasebe/Faturalar/Index");

        if (invoice.IsCancelled)
        {
            UploadMessage = "İptal edilmiş faturaya dosya eklenemez.";
            return RedirectToPage(new { Id });
        }

        if (file == null || file.Length == 0)
        {
            UploadMessage = "Dosya seçin.";
            return RedirectToPage(new { Id });
        }

        if (file.Length > _storageOptions.MaxAttachmentSizeBytes)
        {
            UploadMessage = "Dosya boyutu izin verilen sınırı aşıyor.";
            return RedirectToPage(new { Id });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var tempPath = Path.Combine(Path.GetTempPath(), "balonpark-inv-" + Guid.NewGuid().ToString("N") + (string.IsNullOrEmpty(ext) ? ".bin" : ext));

        try
        {
            byte[] hashBytes;
            long totalWritten;

            {
                await using var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var src = file.OpenReadStream();
                using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > _storageOptions.MaxAttachmentSizeBytes)
                        throw new InvalidOperationException("Dosya boyutu sınırı aşıldı.");

                    sha.AppendData(buffer.AsSpan(0, read));
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }

                hashBytes = sha.GetHashAndReset();
                totalWritten = total;
                await target.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            var shaHex = Convert.ToHexString(hashBytes);

            string storageKey;
            await using (var uploadFs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                storageKey = await _blobStorage.SaveAsync(companyId, uploadFs, file.FileName, file.ContentType ?? "application/octet-stream", cancellationToken).ConfigureAwait(false);
            }

            var adminId = HttpContext.Session.GetAdminId();
            var entity = new InvoiceAttachment
            {
                InvoiceId = Id,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
                StorageKey = storageKey,
                FileSizeBytes = totalWritten,
                Sha256Hex = shaHex,
                UploadedByAdminId = adminId
            };

            await _attachmentRepository.InsertAsync(entity, cancellationToken).ConfigureAwait(false);
            UploadMessage = "Dosya yüklendi.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fatura ek yükleme başarısız. InvoiceId={InvoiceId}, CompanyId={CompanyId}", Id, companyId);
            UploadMessage = "Dosya yüklenemedi. Uzantı, boyut veya dosya türü kurallarına uyun.";
        }
        finally
        {
            try
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
            catch
            {
                /* ignore temp cleanup */
            }
        }

        return RedirectToPage(new { Id });
    }
}
