using Dapper;

namespace BalonPark.Data;

/// <summary>
/// Dapper kullanımına örnek repository sınıfı
/// </summary>
public class ExampleRepository(DapperContext context)
{

    // Örnek: Tüm kayıtları getir
    public async Task<IEnumerable<T>> GetAllAsync<T>(string tableName)
    {
        var query = $"SELECT * FROM {tableName}";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<T>(query);
    }

    // Örnek: ID'ye göre tek kayıt getir
    public async Task<T?> GetByIdAsync<T>(string tableName, int id)
    {
        var query = $"SELECT * FROM {tableName} WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(query, new { Id = id });
    }

    // Örnek: Yeni kayıt ekle
    public async Task<int> InsertAsync<T>(string tableName, T entity)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => p.Name);
        
        var columns = string.Join(", ", properties);
        var values = string.Join(", ", properties.Select(p => $"@{p}"));
        
        var query = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        return await connection.QuerySingleAsync<int>(query, entity);
    }

    // Örnek: Kayıt güncelle
    public async Task<int> UpdateAsync<T>(string tableName, T entity, int id)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => $"{p.Name} = @{p.Name}");
        
        var setClause = string.Join(", ", properties);
        var query = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        return await connection.ExecuteAsync(query, new { Id = id, entity });
    }

    // Örnek: Kayıt sil
    public async Task<int> DeleteAsync(string tableName, int id)
    {
        var query = $"DELETE FROM {tableName} WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        return await connection.ExecuteAsync(query, new { Id = id });
    }

    // Örnek: Stored Procedure çağır
    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string procedureName, object? parameters = null)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<T>(
            procedureName,
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }
}


