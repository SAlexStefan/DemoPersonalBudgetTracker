using Microsoft.Extensions.Diagnostics.HealthChecks;
using PersonalBudgetTrackerApi.Data;

namespace PersonalBudgetTrackerApi.Healthchecks;

public class PostgresHealthCheck : IHealthCheck
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public PostgresHealthCheck(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.ExecuteScalar();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}