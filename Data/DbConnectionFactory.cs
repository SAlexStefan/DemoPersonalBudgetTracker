using Npgsql;
using System.Data;

namespace PersonalBudgetTrackerApi.Data;

/// <summary>
/// Creates a new NpgsqlConnection per request. Registered as scoped so each
/// request/repository call gets its own connection, opened lazily by Dapper.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")?? throw new InvalidOperationException("Connection String is not configured");
    }

    public IDbConnection CreateConnection()=> new NpgsqlConnection(_connectionString);

    /*public IDbConnection CreateConnection()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }*/
}
