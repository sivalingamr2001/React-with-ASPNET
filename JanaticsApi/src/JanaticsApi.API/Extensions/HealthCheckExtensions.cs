// src/EnterpriseApi.API/Extensions/HealthCheckExtensions.cs
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace JanaticsApi.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                healthQuery: "SELECT 1;",
                name: "sqlserver",
                tags: ["database", "critical"])
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: ["cache", "critical"])
            .AddCheck<CustomApplicationHealthCheck>(
                "application",
                tags: ["application"]);

        return services;
    }

    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        // Detailed health for internal monitoring
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        }).RequireAuthorization(PolicyNames.RequireAdminRole);

        // Simple liveness probe for Kubernetes
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Just check the application is running
        });

        // Readiness probe for Kubernetes — checks external dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("critical")
        });

        return app;
    }
}