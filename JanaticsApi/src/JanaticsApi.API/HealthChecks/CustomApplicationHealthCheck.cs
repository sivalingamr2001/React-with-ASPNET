using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JanaticsApi.API.HealthChecks;

public sealed class CustomApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Application is running."));
}
