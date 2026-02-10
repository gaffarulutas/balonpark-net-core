using System.Data;
using Microsoft.Data.SqlClient;

namespace BalonPark.Data;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}


