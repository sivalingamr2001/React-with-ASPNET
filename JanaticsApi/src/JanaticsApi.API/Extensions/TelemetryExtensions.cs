// src/EnterpriseApi.API/Extensions/TelemetryExtensions.cs
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JanaticsApi.API.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddApiTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "EnterpriseApi";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = false; // Prevent query logging PII
                    options.SetDbStatementForStoredProcedures = false;
                })
                .AddRedisInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("EnterpriseApi.Application")
                .AddPrometheusExporter());

        return services;
    }
}