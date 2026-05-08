using BalonPark.Models.Accounting;
using Dapper;

namespace BalonPark.Data;

public class AccountingCompanyRepository(DapperContext context)
{
    public async Task<IReadOnlyList<AccountingCompany>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, LegalName, TaxId, TaxOffice, DefaultCurrency, Address, Phone, Email, IsActive, CreatedAt, UpdatedAt
            FROM AccountingCompanies
            ORDER BY LegalName
            """;
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<AccountingCompany>(new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<AccountingCompany?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, LegalName, TaxId, TaxOffice, DefaultCurrency, Address, Phone, Email, IsActive, CreatedAt, UpdatedAt
            FROM AccountingCompanies WHERE Id = @Id
            """;
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AccountingCompany>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> ExistsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM AccountingCompanies WHERE Id = @Id AND IsActive = 1) THEN 1 ELSE 0 END AS bit)";
        using var connection = context.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> InsertAsync(AccountingCompany company, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AccountingCompanies (LegalName, TaxId, TaxOffice, DefaultCurrency, Address, Phone, Email, IsActive, CreatedAt)
            VALUES (@LegalName, @TaxId, @TaxOffice, @DefaultCurrency, @Address, @Phone, @Email, @IsActive, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int)
            """;
        using var connection = context.CreateConnection();
        return await connection.QuerySingleAsync<int>(new CommandDefinition(sql, company, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> UpdateAsync(AccountingCompany company, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE AccountingCompanies SET
                LegalName = @LegalName,
                TaxId = @TaxId,
                TaxOffice = @TaxOffice,
                DefaultCurrency = @DefaultCurrency,
                Address = @Address,
                Phone = @Phone,
                Email = @Email,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """;
        using var connection = context.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, company, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
