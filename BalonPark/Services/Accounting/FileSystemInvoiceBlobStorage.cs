using Microsoft.Extensions.Options;

namespace BalonPark.Services.Accounting;

/// <summary>MVP: fatura eklerini wwwroot altında saklar. İleride Azure Blob vb. ile değiştirilebilir.</summary>
public class FileSystemInvoiceBlobStorage(IWebHostEnvironment env, IOptions<AccountingStorageOptions> options)
    : IInvoiceBlobStorage
{
    private readonly AccountingStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(int companyId, Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (content.Length > _options.MaxAttachmentSizeBytes)
            throw new InvalidOperationException($"Dosya boyutu en fazla {_options.MaxAttachmentSizeBytes} bayt olabilir.");

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext) || ext.Length > 10)
            ext = ".bin";

        ext = ext.ToLowerInvariant();
        if (!IsAllowedExtension(ext))
            throw new InvalidOperationException("Bu dosya türüne izin verilmiyor.");

        if (ext == ".pdf")
        {
            if (!content.CanSeek)
                throw new InvalidOperationException("PDF doğrulaması için akışın konumlanabilir olması gerekir.");

            var pos = content.Position;
            var header = new byte[5];
            var read = await content.ReadAsync(header.AsMemory(0, 5), cancellationToken).ConfigureAwait(false);
            content.Position = pos;
            if (read < 4 || header[0] != (byte)'%' || header[1] != (byte)'P' || header[2] != (byte)'D' || header[3] != (byte)'F')
                throw new InvalidOperationException("Dosya PDF imzası taşımıyor.");
        }

        var rootRel = _options.LocalRootRelativeToWebRoot.Trim('/').Replace('\\', '/');
        var physicalDir = Path.Combine(env.WebRootPath, rootRel.Replace('/', Path.DirectorySeparatorChar), companyId.ToString());
        Directory.CreateDirectory(physicalDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(physicalDir, fileName);

        await using (var fs = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        return $"/{rootRel}/{companyId}/{fileName}";
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
            return Task.FromResult<Stream?>(null);

        var trimmed = storageKey.TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(env.WebRootPath, trimmed));
        var root = Path.GetFullPath(env.WebRootPath);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    private static bool IsAllowedExtension(string ext) =>
        ext is ".pdf" or ".png" or ".jpg" or ".jpeg" or ".webp" or ".xml";
}
