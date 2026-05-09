using Janatics.DataEngine.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Janatics.DataEngine.Api.Infrastructure;

public sealed class DatabaseHealthCheck(IResilientConnectionFactory connectionFactory, DataEngineOptions options) : IHealthCheck
{
    private readonly IResilientConnectionFactory _connectionFactory = connectionFactory;
    private readonly DataEngineOptions _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
            var healthy = await _connectionFactory.ValidateConnectionHealthAsync(connection).ConfigureAwait(false);
            return healthy ? HealthCheckResult.Healthy("Database is reachable.") : HealthCheckResult.Unhealthy("Database connection is not healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
