using BalonPark.Models.Accounting;
using Dapper;

namespace BalonPark.Data;

public class CounterpartyRepository(DapperContext context)
{
    public async Task<IReadOnlyList<Counterparty>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, CompanyId, Name, CounterpartyType, TaxId, Email, Phone, Notes, IsActive, CreatedAt, UpdatedAt
            FROM Counterparties
            WHERE CompanyId = @CompanyId
            ORDER BY Name
            """;
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<Counterparty>(
            new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<Counterparty?> GetByIdForCompanyAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, CompanyId, Name, CounterpartyType, TaxId, Email, Phone, Notes, IsActive, CreatedAt, UpdatedAt
            FROM Counterparties WHERE Id = @Id AND CompanyId = @CompanyId
            """;
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Counterparty>(
            new CommandDefinition(sql, new { Id = id, CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> InsertAsync(Counterparty entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Counterparties (CompanyId, Name, CounterpartyType, TaxId, Email, Phone, Notes, IsActive, CreatedAt)
            VALUES (@CompanyId, @Name, @CounterpartyType, @TaxId, @Email, @Phone, @Notes, @IsActive, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int)
            """;
        using var connection = context.CreateConnection();
        return await connection.QuerySingleAsync<int>(new CommandDefinition(sql, entity, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> UpdateAsync(Counterparty entity, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Counterparties SET
                Name = @Name,
                CounterpartyType = @CounterpartyType,
                TaxId = @TaxId,
                Email = @Email,
                Phone = @Phone,
                Notes = @Notes,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND CompanyId = @CompanyId
            """;
        using var connection = context.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            entity.Id,
            entity.Name,
            entity.CounterpartyType,
            entity.TaxId,
            entity.Email,
            entity.Phone,
            entity.Notes,
            entity.IsActive,
            CompanyId = companyId
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<Counterparty?> GetByTaxIdAsync(string taxId, int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, CompanyId, Name, CounterpartyType, TaxId, Email, Phone, Notes, IsActive, CreatedAt, UpdatedAt
            FROM Counterparties
            WHERE CompanyId = @CompanyId AND TaxId = @TaxId
            """;
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Counterparty>(
            new CommandDefinition(sql, new { CompanyId = companyId, TaxId = taxId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> CountByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Counterparties WHERE CompanyId = @CompanyId AND IsActive = 1";
        using var connection = context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
