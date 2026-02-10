using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BalonPark.Data;

/// <summary>
/// Uygulama başlarken Migrations klasöründeki SQL scriptlerini sırayla çalıştırır.
/// __MigrationsHistory tablosu ile hangi scriptin çalıştığını takip eder.
/// </summary>
public class SqlMigrationRunner
{
    private const string HistoryTableName = "__MigrationsHistory";
    private const string MigrationsFolder = "Migrations";

    private readonly DapperContext _context;
    private readonly ILogger<SqlMigrationRunner> _logger;
    private readonly IHostEnvironment _env;

    public SqlMigrationRunner(
        DapperContext context,
        ILogger<SqlMigrationRunner> logger,
        IHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Migrations klasöründeki .sql dosyalarını sırayla çalıştırır (henüz çalışmamış olanlar).
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Önce output (bin) içindeki Migrations, yoksa ContentRootPath (proje kökü)
        var basePath = AppContext.BaseDirectory;
        var migrationsPath = Path.Combine(basePath, MigrationsFolder);
        if (!Directory.Exists(migrationsPath))
            migrationsPath = Path.Combine(_env.ContentRootPath ?? basePath, MigrationsFolder);
        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Migrations klasörü bulunamadı: {Path}", migrationsPath);
            return;
        }

        var files = Directory
            .GetFiles(migrationsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileName(f))
            .Where(f => !string.IsNullOrEmpty(f) && f!.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f!, StringComparer.Ordinal)
            .Select(f => f!)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogInformation("Çalıştırılacak migration dosyası yok.");
            return;
        }

        await EnsureHistoryTableAsync(cancellationToken).ConfigureAwait(false);
        var applied = await GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var fileName in files)
        {
            if (applied.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Migration zaten uygulandı: {File}", fileName);
                continue;
            }

            var fullPath = Path.Combine(migrationsPath, fileName);
            var script = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);

            try
            {
                await ExecuteScriptAsync(fileName, script, cancellationToken).ConfigureAwait(false);
                await RecordMigrationAsync(fileName, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Migration uygulandı: {File}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration başarısız: {File}", fileName);
                throw;
            }
        }
    }

    private async Task EnsureHistoryTableAsync(CancellationToken cancellationToken)
    {
        var sql = $@"
IF OBJECT_ID('dbo.{HistoryTableName}', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[{HistoryTableName}] (
        [MigrationId] NVARCHAR(150) NOT NULL PRIMARY KEY,
        [AppliedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE()
    );
END";
        await ExecuteBatchAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var connection = (SqlConnection)_context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT [MigrationId] FROM [dbo].[{HistoryTableName}]";
        cmd.CommandType = CommandType.Text;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetString(0);
            result.Add(id);
        }

        return result;
    }

    private async Task RecordMigrationAsync(string migrationId, CancellationToken cancellationToken)
    {
        var sql = $"INSERT INTO [dbo].[{HistoryTableName}] ([MigrationId]) VALUES (@id)";
        await using var connection = (SqlConnection)_context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@id", migrationId));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteScriptAsync(string fileName, string script, CancellationToken cancellationToken)
    {
        var batches = SplitBatches(script);
        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || IsOnlyComments(trimmed))
                continue;
            await ExecuteBatchAsync(trimmed, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsOnlyComments(string sql)
    {
        var lines = sql.Split('\n', '\r');
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (string.IsNullOrEmpty(t)) continue;
            if (t.StartsWith("--", StringComparison.Ordinal)) continue;
            return false;
        }
        return true;
    }

    /// <summary>
    /// SQL Server GO batch ayırıcısına göre scripti parçalara böler.
    /// </summary>
    private static IEnumerable<string> SplitBatches(string script)
    {
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (current.Count > 0)
                {
                    yield return string.Join(Environment.NewLine, current);
                    current.Clear();
                }
            }
            else
            {
                current.Add(line);
            }
        }
        if (current.Count > 0)
            yield return string.Join(Environment.NewLine, current);
    }

    private async Task ExecuteBatchAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = (SqlConnection)_context.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
