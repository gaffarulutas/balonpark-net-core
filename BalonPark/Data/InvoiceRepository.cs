using BalonPark.Models.Accounting;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BalonPark.Data;

public class InvoiceRepository(DapperContext context)
{
    public async Task<IReadOnlyList<Invoice>> GetByCompanyAsync(int companyId, int take = 500, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take)
                i.Id, i.CompanyId, i.CounterpartyId, i.Direction, i.InvoiceNo, i.IssueDate, i.DueDate, i.Currency,
                i.AmountNet, i.AmountVat, i.AmountGross, i.IsCancelled, i.Notes, i.CreatedByAdminId, i.CreatedAt, i.UpdatedAt,
                c.Name AS CounterpartyName
            FROM Invoices i
            INNER JOIN Counterparties c ON c.Id = i.CounterpartyId AND c.CompanyId = i.CompanyId
            WHERE i.CompanyId = @CompanyId
            ORDER BY i.IssueDate DESC, i.Id DESC
            """;
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<Invoice>(
            new CommandDefinition(sql, new { CompanyId = companyId, Take = take }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<Invoice?> GetByIdForCompanyAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT i.Id, i.CompanyId, i.CounterpartyId, i.Direction, i.InvoiceNo, i.IssueDate, i.DueDate, i.Currency,
                i.AmountNet, i.AmountVat, i.AmountGross, i.IsCancelled, i.Notes, i.CreatedByAdminId, i.CreatedAt, i.UpdatedAt,
                c.Name AS CounterpartyName
            FROM Invoices i
            INNER JOIN Counterparties c ON c.Id = i.CounterpartyId AND c.CompanyId = i.CompanyId
            WHERE i.Id = @Id AND i.CompanyId = @CompanyId
            """;
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Invoice>(
            new CommandDefinition(sql, new { Id = id, CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Fatura + cari hareketini tek işlemde yazar. Cari aynı şirkete ait ve aktif olmalı.</summary>
    public async Task<int> CreateWithMovementAsync(Invoice invoice, int adminId, CancellationToken cancellationToken = default)
    {
        await using var connection = (SqlConnection)context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        const string validateCari = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM Counterparties
                WHERE Id = @CounterpartyId AND CompanyId = @CompanyId AND IsActive = 1
            ) THEN 1 ELSE 0 END AS bit)
            """;

        var cariOk = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(validateCari, new { invoice.CounterpartyId, invoice.CompanyId }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (!cariOk)
            throw new InvalidOperationException("Cari bulunamadı, pasif veya bu şirkete ait değil.");

        const string insertInvoice = """
            INSERT INTO Invoices (CompanyId, CounterpartyId, Direction, InvoiceNo, IssueDate, DueDate, Currency,
                AmountNet, AmountVat, AmountGross, Notes, CreatedByAdminId, CreatedAt, IsCancelled)
            VALUES (@CompanyId, @CounterpartyId, @Direction, @InvoiceNo, @IssueDate, @DueDate, @Currency,
                @AmountNet, @AmountVat, @AmountGross, @Notes, @CreatedByAdminId, SYSUTCDATETIME(), 0);
            SELECT CAST(SCOPE_IDENTITY() AS int)
            """;

        try
        {
            var invoiceId = await connection.QuerySingleAsync<int>(
                new CommandDefinition(insertInvoice, new
                {
                    invoice.CompanyId,
                    invoice.CounterpartyId,
                    Direction = (byte)invoice.Direction,
                    invoice.InvoiceNo,
                    IssueDate = invoice.IssueDate.Date,
                    DueDate = invoice.DueDate?.Date,
                    Currency = invoice.Currency.Trim().Length >= 3 ? invoice.Currency.Trim().Substring(0, 3) : "TRY",
                    invoice.AmountNet,
                    invoice.AmountVat,
                    invoice.AmountGross,
                    invoice.Notes,
                    CreatedByAdminId = adminId
                }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            var isCredit = invoice.Direction == InvoiceDirection.Outgoing;
            var desc = invoice.Direction == InvoiceDirection.Outgoing
                ? $"Satış faturası {invoice.InvoiceNo}"
                : $"Alış faturası {invoice.InvoiceNo}";

            const string insertMovement = """
                INSERT INTO AccountMovements (CompanyId, CounterpartyId, MovementDate, IsCredit, Amount, Currency, ReferenceType, ReferenceId, Description, CreatedByAdminId, CreatedAt)
                VALUES (@CompanyId, @CounterpartyId, @MovementDate, @IsCredit, @Amount, @Currency, @ReferenceType, @ReferenceId, @Description, @CreatedByAdminId, SYSUTCDATETIME())
                """;

            var cur = invoice.Currency.Trim().Length >= 3 ? invoice.Currency.Trim().Substring(0, 3) : "TRY";

            await connection.ExecuteAsync(
                new CommandDefinition(insertMovement, new
                {
                    invoice.CompanyId,
                    invoice.CounterpartyId,
                    MovementDate = invoice.IssueDate.Date,
                    IsCredit = isCredit,
                    Amount = invoice.AmountGross,
                    Currency = cur,
                    ReferenceType = AccountingConstants.ReferenceTypeInvoice,
                    ReferenceId = invoiceId,
                    Description = desc,
                    CreatedByAdminId = adminId
                }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return invoiceId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>İptal: ters cari hareketi yazar ve faturayı iptal işaretler.</summary>
    public async Task<bool> CancelInvoiceAsync(int invoiceId, int companyId, int adminId, CancellationToken cancellationToken = default)
    {
        await using var connection = (SqlConnection)context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        const string selectSql = """
            SELECT Direction, AmountGross, Currency, InvoiceNo, CounterpartyId, IssueDate, IsCancelled
            FROM Invoices WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @Id AND CompanyId = @CompanyId
            """;

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<InvoiceCancelRow>(
                new CommandDefinition(selectSql, new { Id = invoiceId, CompanyId = companyId }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (row is null || row.IsCancelled)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            var inv = row;
            var origIsCredit = inv.Direction == (byte)InvoiceDirection.Outgoing;
            var reverseIsCredit = !origIsCredit;
            var cur = inv.Currency.Trim().Length >= 3 ? inv.Currency.Trim().Substring(0, 3) : "TRY";

            const string insertMovement = """
                INSERT INTO AccountMovements (CompanyId, CounterpartyId, MovementDate, IsCredit, Amount, Currency, ReferenceType, ReferenceId, Description, CreatedByAdminId, CreatedAt)
                VALUES (@CompanyId, @CounterpartyId, @MovementDate, @IsCredit, @Amount, @Currency, @ReferenceType, @ReferenceId, @Description, @CreatedByAdminId, SYSUTCDATETIME())
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(insertMovement, new
                {
                    CompanyId = companyId,
                    CounterpartyId = inv.CounterpartyId,
                    MovementDate = inv.IssueDate.Date,
                    IsCredit = reverseIsCredit,
                    Amount = inv.AmountGross,
                    Currency = cur,
                    ReferenceType = AccountingConstants.ReferenceTypeInvoiceCancel,
                    ReferenceId = invoiceId,
                    Description = $"Fatura iptal: {inv.InvoiceNo}",
                    CreatedByAdminId = adminId
                }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string updateSql = """
                UPDATE Invoices SET IsCancelled = 1, UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id AND CompanyId = @CompanyId AND IsCancelled = 0
                """;

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(updateSql, new { Id = invoiceId, CompanyId = companyId }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (updated == 0)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<int> CountActiveByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Invoices WHERE CompanyId = @CompanyId AND IsCancelled = 0";
        using var connection = context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private sealed class InvoiceCancelRow
    {
        public byte Direction { get; set; }
        public decimal AmountGross { get; set; }
        public string Currency { get; set; } = "TRY";
        public string InvoiceNo { get; set; } = string.Empty;
        public int CounterpartyId { get; set; }
        public DateTime IssueDate { get; set; }
        public bool IsCancelled { get; set; }
    }
}
