using FluentFTP;

namespace BalonPark.Services.CatalogSync;

public interface ITargetUploadSync
{
    Task SyncProductFolderAsync(CatalogSyncTargetOptions target, int productId, CancellationToken cancellationToken = default);
    Task DeleteProductFolderAsync(CatalogSyncTargetOptions target, int productId, CancellationToken cancellationToken = default);
}

public sealed class TargetUploadSync(
    IWebHostEnvironment environment,
    ILogger<TargetUploadSync> logger) : ITargetUploadSync
{
    public async Task SyncProductFolderAsync(CatalogSyncTargetOptions target, int productId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.FtpHost) || string.IsNullOrWhiteSpace(target.FtpUser) || string.IsNullOrWhiteSpace(target.FtpPass))
        {
            logger.LogWarning("CatalogSync FTP skipped for {Target}: credentials missing", target.Name);
            return;
        }

        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));

        var localDir = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "products", productId.ToString()));
        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "products"));
        if (!localDir.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(localDir, uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid product upload path.");
        }

        if (!Directory.Exists(localDir))
        {
            logger.LogInformation("CatalogSync FTP: no local folder for product {Id}", productId);
            return;
        }

        var files = Directory.GetFiles(localDir, "*", SearchOption.AllDirectories);
        if (files.Length == 0)
            return;

        var remoteRoot = NormalizeRemote(target.FtpUploadsRoot);
        var remoteDir = Combine(remoteRoot, "uploads", "products", productId.ToString());

        await using var client = CreateClient(target);
        await client.Connect(cancellationToken).ConfigureAwait(false);
        await client.CreateDirectory(remoteDir, true, cancellationToken).ConfigureAwait(false);

        foreach (var file in files)
        {
            var fullFile = Path.GetFullPath(file);
            if (!fullFile.StartsWith(localDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullFile, localDir, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("CatalogSync FTP: skipped file outside product folder: {File}", file);
                continue;
            }

            var rel = Path.GetRelativePath(localDir, fullFile).Replace('\\', '/');
            if (rel.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
            {
                logger.LogWarning("CatalogSync FTP: skipped unsafe relative path {Rel}", rel);
                continue;
            }

            var remotePath = Combine(remoteDir, rel);
            var remoteParent = remotePath.Contains('/') ? remotePath[..remotePath.LastIndexOf('/')] : remoteDir;
            await client.CreateDirectory(remoteParent, true, cancellationToken).ConfigureAwait(false);

            await using var stream = File.OpenRead(fullFile);
            var status = await client.UploadStream(stream, remotePath, FtpRemoteExists.Overwrite, true, token: cancellationToken)
                .ConfigureAwait(false);
            if (status is not (FtpStatus.Success or FtpStatus.Skipped))
                throw new InvalidOperationException($"FTP upload failed for {remotePath}: {status}");
        }

        logger.LogInformation("CatalogSync FTP: uploaded {Count} files for product {Id} → {Target}", files.Length, productId, target.Name);
    }

    public async Task DeleteProductFolderAsync(CatalogSyncTargetOptions target, int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
            return;
        if (string.IsNullOrWhiteSpace(target.FtpHost) || string.IsNullOrWhiteSpace(target.FtpUser) || string.IsNullOrWhiteSpace(target.FtpPass))
            return;

        var remoteRoot = NormalizeRemote(target.FtpUploadsRoot);
        var remoteDir = Combine(remoteRoot, "uploads", "products", productId.ToString());

        try
        {
            await using var client = CreateClient(target);
            await client.Connect(cancellationToken).ConfigureAwait(false);
            if (await client.DirectoryExists(remoteDir, cancellationToken).ConfigureAwait(false))
                await client.DeleteDirectory(remoteDir, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CatalogSync FTP: could not delete product folder {Id} on {Target}", productId, target.Name);
        }
    }

    private static AsyncFtpClient CreateClient(CatalogSyncTargetOptions target)
    {
        var client = new AsyncFtpClient(target.FtpHost.Trim(), target.FtpUser.Trim(), target.FtpPass, target.FtpPort <= 0 ? 21 : target.FtpPort);
        client.Config.EncryptionMode = FtpEncryptionMode.None;
        client.Config.ConnectTimeout = 30000;
        client.Config.ReadTimeout = 60000;
        client.Config.DataConnectionConnectTimeout = 30000;
        client.Config.DataConnectionReadTimeout = 60000;
        return client;
    }

    private static string NormalizeRemote(string path)
    {
        var normalized = (path ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrEmpty(normalized))
            return "/";
        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;
        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    private static string Combine(params string[] parts)
    {
        var list = parts
            .SelectMany(p => (p ?? string.Empty).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            .Where(p => p is not "." and not "..")
            .ToList();
        return "/" + string.Join('/', list);
    }
}
