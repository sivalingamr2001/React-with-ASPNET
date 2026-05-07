using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using JanaticsApi.API.Services;
using JanaticsApi.Shared.Constants;
using JanaticsApi.Shared.Settings;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace JanaticsApi.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddApiCors(configuration);

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/json", "application/problem+json" });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
            options.Level = CompressionLevel.Optimal);

        services.Configure<GzipCompressionProviderOptions>(options =>
            options.Level = CompressionLevel.SmallestSize);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version"),
                new MediaTypeApiVersionReader("ver"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddApiSwagger();
        services.AddApiRateLimiting();
        services.AddApiAuthentication(configuration);
        services.AddApiHealthChecks(configuration);
        services.AddApiTelemetry(configuration);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
