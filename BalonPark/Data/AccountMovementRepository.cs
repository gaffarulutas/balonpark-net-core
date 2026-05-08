using BalonPark.Models.Accounting;
using Dapper;

namespace BalonPark.Data;

public class AccountMovementRepository(DapperContext context)
{
    public async Task<IReadOnlyList<AccountMovement>> GetByCompanyAsync(int companyId, int? counterpartyId, int take, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take)
                m.Id, m.CompanyId, m.CounterpartyId, m.MovementDate, m.IsCredit, m.Amount, m.Currency,
                m.ReferenceType, m.ReferenceId, m.Description, m.CreatedByAdminId, m.CreatedAt,
                c.Name AS CounterpartyName
            FROM AccountMovements m
            INNER JOIN Counterparties c ON c.Id = m.CounterpartyId AND c.CompanyId = m.CompanyId
            WHERE m.CompanyId = @CompanyId
              AND (@CounterpartyId IS NULL OR m.CounterpartyId = @CounterpartyId)
            ORDER BY m.MovementDate DESC, m.Id DESC
            """;
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<AccountMovement>(
            new CommandDefinition(sql, new { CompanyId = companyId, CounterpartyId = counterpartyId, Take = take }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<(decimal DebitTotal, decimal CreditTotal)> GetTotalsForCounterpartyAsync(int companyId, int counterpartyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ISNULL(SUM(CASE WHEN IsCredit = 0 THEN Amount ELSE 0 END), 0) AS DebitTotal,
                ISNULL(SUM(CASE WHEN IsCredit = 1 THEN Amount ELSE 0 END), 0) AS CreditTotal
            FROM AccountMovements
            WHERE CompanyId = @CompanyId AND CounterpartyId = @CounterpartyId
            """;
        using var connection = context.CreateConnection();
        var row = await connection.QueryFirstAsync<(decimal DebitTotal, decimal CreditTotal)>(
            new CommandDefinition(sql, new { CompanyId = companyId, CounterpartyId = counterpartyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row;
    }

    public async Task<int> InsertAsync(AccountMovement movement, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AccountMovements (CompanyId, CounterpartyId, MovementDate, IsCredit, Amount, Currency, ReferenceType, ReferenceId, Description, CreatedByAdminId, CreatedAt)
            VALUES (@CompanyId, @CounterpartyId, @MovementDate, @IsCredit, @Amount, @Currency, @ReferenceType, @ReferenceId, @Description, @CreatedByAdminId, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int)
            """;
        using var connection = context.CreateConnection();
        return await connection.QuerySingleAsync<int>(new CommandDefinition(sql, movement, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
