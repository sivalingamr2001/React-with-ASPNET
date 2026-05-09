using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Core;
using Janatics.DataEngine.FetchService.Infrastructure.Persistence;
using Janatics.DataEngine.FetchServiceInfrastructure.Persistence;
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Core.Auditing;
using Janatics.DataEngine.ProcessService.Core.Mapping;
using Janatics.DataEngine.ProcessService.Core.Processing;
using Janatics.DataEngine.ProcessService.Infrastructure.BackgroundJobs;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Infrastructure.Providers;
using Janatics.DataEngine.ProcessService.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine;

public static class DataEngineServiceExtensions
{
    public static IServiceCollection AddJanaticsDataEngine(this IServiceCollection services, Action<DataEngineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Phase 1 fail-fast validation before registrations.
        var options = new DataEngineOptions();
        configure(options);
        NormalizeAndValidateOptions(options);

        services
            .AddOptions<DataEngineOptions>()
            .Configure(configure)
            .PostConfigure(NormalizeAndValidateOptions)
            .ValidateOnStart();

        // Backward-compatible concrete options resolution for existing service constructors.
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DataEngineOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DataEngineOptions>>().Value.DatabaseConfig);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DataEngineOptions>>().Value.FetchService);

        // Infrastructure and provider registrations.
        services.AddSingleton<IResilientConnectionFactory, ResilientConnectionFactory>();
        services.AddSingleton<IProviderCapabilityRegistry, ProviderCapabilityRegistry>();
        services.AddScoped<IDataProvider>(sp =>
        {
            var db = sp.GetRequiredService<DatabaseConfig>();
            return db.Provider switch
            {
                DatabaseProvider.PostgreSQL => ActivatorUtilities.CreateInstance<PostgreSqlProvider>(sp),
                DatabaseProvider.SqlServer => ActivatorUtilities.CreateInstance<SqlServerProvider>(sp),
                DatabaseProvider.MySQL => ActivatorUtilities.CreateInstance<MySqlProvider>(sp),
                DatabaseProvider.Oracle => ActivatorUtilities.CreateInstance<OracleProvider>(sp),
                DatabaseProvider.Sqlite => ActivatorUtilities.CreateInstance<SqliteProvider>(sp),
                _ => throw new NotSupportedException($"Provider '{db.Provider}' is not yet registered for IDataProvider runtime usage.")
            };
        });
        services.AddScoped<IQueryDefinitionRepository, QueryDefinitionRepository>();
        services.AddSingleton<IMasterTablePreflight, MasterTablePreflight>();
        services.AddSingleton<IActionFileLogger, ActionFileLogger>();
        services.AddMemoryCache();
        services.AddSingleton<IQueryPlanCompiler, DefaultQueryPlanCompiler>();
        services.AddSingleton<IQueryPlanCache, MemoryQueryPlanCache>();
        services.AddSingleton<ISqlValidator, SqlValidator>();
        services.AddScoped<IFetchService, global::Janatics.DataEngine.FetchService.Core.FetchService>();

        // Core services.
        services.AddSingleton<IAuditQueue, AuditQueue>();
        services.AddScoped<FieldMapperService>();
        services.AddScoped<DataTypeConverter>();
        services.AddSingleton<IDeterministicIdGenerator, DeterministicIdGenerator>();
        services.AddScoped<AuditDiffBuilder>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuditWriter>();
        services.AddScoped<IAuditOutboxService, AuditOutboxService>();
        services.AddHostedService<AuditOutboxBackgroundService>();
        services.AddScoped<IProcessService, CoreProcessService>();
        services.AddScoped<IValidationService, ValidationService>();

        // Main entry point.
        services.AddScoped<DataEngine>();

        // Auto-create master tables on first use via warm-up service
        services.AddHostedService<DataEngineWarmupService>();

        return services;
    }

    private static void NormalizeAndValidateOptions(DataEngineOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FetchService.DatabaseConfig.ConnectionString))
            options.FetchService.DatabaseConfig = options.DatabaseConfig;

        if (string.IsNullOrWhiteSpace(options.DatabaseConfig.ConnectionString))
            throw new InvalidOperationException("DatabaseConfig.ConnectionString is required.");

        if (options.OperationTimeoutSeconds <= 0)
            throw new InvalidOperationException("OperationTimeoutSeconds must be greater than zero.");

        if (options.MaxRetryCount < 0)
            throw new InvalidOperationException("MaxRetryCount cannot be negative.");

        if (options.FetchService.DefaultPageSize <= 0 || options.FetchService.MaxPageSize <= 0)
            throw new InvalidOperationException("FetchService page sizes must be greater than zero.");

        if (options.FetchService.DefaultPageSize > options.FetchService.MaxPageSize)
            throw new InvalidOperationException("FetchService.DefaultPageSize must be less than or equal to FetchService.MaxPageSize.");
    }
}