using FluentFTP;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BalonPark.Services.Accounting;

public class FtpInvoiceBlobStorage(IOptions<AccountingStorageOptions> options) : IInvoiceBlobStorage
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
            await ValidatePdfSignatureAsync(content, cancellationToken).ConfigureAwait(false);

        var host = _options.FtpHost?.Trim();
        var username = _options.FtpUsername?.Trim();
        var password = _options.FtpPassword;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("FTP ayarları eksik. Host, kullanıcı adı ve şifre zorunludur.");

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var normalizedRoot = NormalizeRemotePath(_options.FtpRootPath);
        var remoteDirectory = CombineRemotePath(normalizedRoot, companyId.ToString(CultureInfo.InvariantCulture));
        var remotePath = CombineRemotePath(remoteDirectory, fileName);

        if (content.CanSeek)
            content.Position = 0;

        await using var client = CreateClient(host, username, password);
        await client.Connect(cancellationToken).ConfigureAwait(false);
        await client.CreateDirectory(remoteDirectory, true, cancellationToken).ConfigureAwait(false);

        var uploadStatus = await client.UploadStream(content, remotePath, FtpRemoteExists.NoCheck, true, null, cancellationToken).ConfigureAwait(false);
        if (uploadStatus is not (FtpStatus.Success or FtpStatus.Skipped))
            throw new InvalidOperationException("Dosya FTP sunucusuna yüklenemedi.");

        return remotePath;
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
            return null;

        var host = _options.FtpHost?.Trim();
        var username = _options.FtpUsername?.Trim();
        var password = _options.FtpPassword;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var normalizedStorageKey = NormalizeRemotePath(storageKey);
        var normalizedRoot = NormalizeRemotePath(_options.FtpRootPath);
        if (!IsUnderRoot(normalizedStorageKey, normalizedRoot))
            return null;

        await using var client = CreateClient(host, username, password);
        await client.Connect(cancellationToken).ConfigureAwait(false);

        var memory = new MemoryStream();
        var downloadSucceeded = await client.DownloadStream(memory, normalizedStorageKey, 0, null, cancellationToken).ConfigureAwait(false);
        if (!downloadSucceeded)
        {
            await memory.DisposeAsync().ConfigureAwait(false);
            return null;
        }

        memory.Position = 0;
        return memory;
    }

    private AsyncFtpClient CreateClient(string host, string username, string password)
    {
        var client = new AsyncFtpClient(host, username, password, _options.FtpPort);
        client.Config.EncryptionMode = _options.FtpUseSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
        client.Config.ValidateAnyCertificate = _options.FtpAcceptAnyCertificate;
        client.Config.ConnectTimeout = 15000;
        client.Config.ReadTimeout = 15000;
        client.Config.DataConnectionConnectTimeout = 15000;
        client.Config.DataConnectionReadTimeout = 15000;
        return client;
    }

    private static async Task ValidatePdfSignatureAsync(Stream content, CancellationToken cancellationToken)
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

    private static string NormalizeRemotePath(string path)
    {
        var normalized = (path ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrEmpty(normalized))
            return "/";

        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;

        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);

        if (normalized.Length > 1)
            normalized = normalized.TrimEnd('/');

        return normalized;
    }

    private static string CombineRemotePath(string parent, string child)
    {
        var safeChild = child.Trim().Trim('/');
        if (string.IsNullOrEmpty(safeChild))
            return NormalizeRemotePath(parent);

        var safeParent = NormalizeRemotePath(parent);
        if (safeParent == "/")
            return "/" + safeChild;

        return safeParent + "/" + safeChild;
    }

    private static bool IsUnderRoot(string storageKey, string root)
    {
        if (root == "/")
            return storageKey.StartsWith("/", StringComparison.Ordinal);

        return storageKey.StartsWith(root + "/", StringComparison.Ordinal);
    }

    private static bool IsAllowedExtension(string ext) =>
        ext is ".pdf" or ".png" or ".jpg" or ".jpeg" or ".webp" or ".xml";
}