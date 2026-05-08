namespace BalonPark.Services.Accounting;

public interface IInvoiceBlobStorage
{
    /// <summary>Tarayıcıdan servis edilebilir göreli yol (örn. /uploads/accounting/1/xxx.pdf).</summary>
    Task<string> SaveAsync(int companyId, Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
