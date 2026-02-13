using Dapper;
using BalonPark.Models;
using Microsoft.Extensions.Logging;

namespace BalonPark.Data;

public class ErrorLogRepository(DapperContext context, ILogger<ErrorLogRepository> logger)
{
    private const string TableName = "ErrorLogs";

    /// <summary>
    /// Sayfalı hata logları listesi (en yeni önce).
    /// </summary>
    public async Task<(IEnumerable<ErrorLog> Items, int TotalCount)> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? level = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        try
        {
            using var connection = context.CreateConnection();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new DynamicParameters();
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Offset", (page - 1) * pageSize);

            if (!string.IsNullOrWhiteSpace(level))
            {
                whereClauses.Add("Level = @Level");
                parameters.Add("@Level", level.Trim());
            }

            if (from.HasValue)
            {
                whereClauses.Add("TimeStamp >= @From");
                parameters.Add("@From", from.Value);
            }

            if (to.HasValue)
            {
                whereClauses.Add("TimeStamp <= @To");
                parameters.Add("@To", to.Value);
            }

            var whereSql = string.Join(" AND ", whereClauses);

            var countSql = $@"
                SELECT COUNT(1) FROM [{TableName}]
                WHERE {whereSql}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var dataSql = $@"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM [{TableName}]
                WHERE {whereSql}
                ORDER BY TimeStamp DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var items = await connection.QueryAsync<ErrorLog>(dataSql, parameters);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ErrorLogs tablosu okunamadı (tablo henüz oluşturulmamış olabilir).");
            return (Enumerable.Empty<ErrorLog>(), 0);
        }
    }

    /// <summary>
    /// Tek bir hata kaydı getirir.
    /// </summary>
    public async Task<ErrorLog?> GetByIdAsync(int id)
    {
        try
        {
            using var connection = context.CreateConnection();
            var sql = $@"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM [{TableName}]
                WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<ErrorLog>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ErrorLogs tablosu okunamadı. Id: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Tüm hata loglarını veritabanından siler (geri alınamaz).
    /// </summary>
    /// <returns>Silinen kayıt sayısı.</returns>
    public async Task<int> DeleteAllAsync()
    {
        try
        {
            using var connection = context.CreateConnection();
            var sql = $@"DELETE FROM [{TableName}]";
            var deleted = await connection.ExecuteAsync(sql);
            if (deleted > 0)
                logger.LogInformation("ErrorLogs: {Count} kayıt silindi.", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ErrorLogs toplu silme hatası.");
            throw;
        }
    }

    /// <summary>
    /// Seviyeye göre log sayıları (özet).
    /// </summary>
    public async Task<Dictionary<string, int>> GetCountByLevelAsync()
    {
        try
        {
            using var connection = context.CreateConnection();
            var sql = $@"
                SELECT Level AS [Key], COUNT(1) AS [Value]
                FROM [{TableName}]
                GROUP BY Level";
            var rows = await connection.QueryAsync<(string Key, int Value)>(sql);
            return rows.ToDictionary(x => x.Key, x => x.Value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ErrorLogs tablosu okunamadı.");
            return new Dictionary<string, int>();
        }
    }
}
