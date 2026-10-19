using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BalonPark.Services;

internal static class PriceSettingsSql
{
    internal const string SelectSnapshot = """
        SELECT TOP 1 CAST(ShowPrices AS bit) AS ShowPrices, PriceRevision
        FROM dbo.Settings
        ORDER BY Id
        """;

    internal const string UpdateFlag = """
        UPDATE dbo.Settings
        SET ShowPrices = @ShowPrices,
            PriceRevision = SYSDATETIME(),
            UpdatedAt = SYSDATETIME()
        """;

    /// <summary>
    /// Price decimal(18,2) * factor, then ROUND to 0 (SQL Server rounds halves away from zero).
    /// Positive prices never fall below 1. Zero and negative prices are left unchanged.
    /// </summary>
    internal const string AdjustPrices = """
        DECLARE @count int;

        UPDATE dbo.Products
        SET Price = CASE
                WHEN ROUND(CAST(Price AS decimal(18, 4)) * CAST(@Factor AS decimal(9, 4)), 0) < 1 THEN CAST(1 AS decimal(18, 2))
                ELSE ROUND(CAST(Price AS decimal(18, 4)) * CAST(@Factor AS decimal(9, 4)), 0)
            END,
            UpdatedAt = SYSDATETIME()
        WHERE Price > 0;

        SET @count = @@ROWCOUNT;

        UPDATE dbo.Settings
        SET PriceRevision = SYSDATETIME(),
            UpdatedAt = SYSDATETIME();

        SELECT @count;
        """;

    internal static async Task<PriceSettingsSnapshot> ReadAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var row = await connection.QueryFirstOrDefaultAsync<PriceSettingsRow>(
            new CommandDefinition(SelectSnapshot, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row == null
            ? new PriceSettingsSnapshot(true, DateTime.UnixEpoch)
            : new PriceSettingsSnapshot(row.ShowPrices, row.PriceRevision);
    }

    internal static async Task SetFlagAsync(SqlConnection connection, bool showPrices, CancellationToken cancellationToken, System.Data.Common.DbTransaction? transaction = null)
    {
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            UpdateFlag,
            new { ShowPrices = showPrices },
            transaction,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (updated == 0)
            throw new InvalidOperationException("Settings satırı bulunamadı.");
    }

    internal static async Task<int> AdjustAsync(SqlConnection connection, decimal factor, CancellationToken cancellationToken, System.Data.Common.DbTransaction? transaction = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Factor", factor, DbType.Decimal, precision: 9, scale: 4);

        return await connection.QuerySingleAsync<int>(new CommandDefinition(
            AdjustPrices,
            parameters,
            transaction,
            commandTimeout: 180,
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    internal static string WithConnectTimeout(string connectionString)
    {
        if (connectionString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        return connectionString.TrimEnd(';') + ";Connect Timeout=10";
    }

    private sealed class PriceSettingsRow
    {
        public bool ShowPrices { get; set; }
        public DateTime PriceRevision { get; set; }
    }
}
