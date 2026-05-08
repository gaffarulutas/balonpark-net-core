using BalonPark.Models.Accounting;
using Dapper;

namespace BalonPark.Data;

public class InvoiceAttachmentRepository(DapperContext context)
{
    public async Task<IReadOnlyList<InvoiceAttachment>> GetByInvoiceForCompanyAsync(int invoiceId, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT a.Id, a.InvoiceId, a.OriginalFileName, a.ContentType, a.StorageKey, a.FileSizeBytes, a.Sha256Hex, a.UploadedByAdminId, a.UploadedAt,
                i.CompanyId
            FROM InvoiceAttachments a
            INNER JOIN Invoices i ON i.Id = a.InvoiceId
            WHERE a.InvoiceId = @InvoiceId AND i.CompanyId = @CompanyId
            ORDER BY a.UploadedAt DESC
            """;
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<InvoiceAttachment>(
            new CommandDefinition(sql, new { InvoiceId = invoiceId, CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<InvoiceAttachment?> GetByIdForCompanyAsync(int attachmentId, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT a.Id, a.InvoiceId, a.OriginalFileName, a.ContentType, a.StorageKey, a.FileSizeBytes, a.Sha256Hex, a.UploadedByAdminId, a.UploadedAt,
                i.CompanyId
            FROM InvoiceAttachments a
            INNER JOIN Invoices i ON i.Id = a.InvoiceId
            WHERE a.Id = @Id AND i.CompanyId = @CompanyId
            """;
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InvoiceAttachment>(
            new CommandDefinition(sql, new { Id = attachmentId, CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> InsertAsync(InvoiceAttachment entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO InvoiceAttachments (InvoiceId, OriginalFileName, ContentType, StorageKey, FileSizeBytes, Sha256Hex, UploadedByAdminId, UploadedAt)
            VALUES (@InvoiceId, @OriginalFileName, @ContentType, @StorageKey, @FileSizeBytes, @Sha256Hex, @UploadedByAdminId, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int)
            """;
        using var connection = context.CreateConnection();
        return await connection.QuerySingleAsync<int>(new CommandDefinition(sql, new
        {
            entity.InvoiceId,
            entity.OriginalFileName,
            entity.ContentType,
            entity.StorageKey,
            entity.FileSizeBytes,
            entity.Sha256Hex,
            entity.UploadedByAdminId
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
