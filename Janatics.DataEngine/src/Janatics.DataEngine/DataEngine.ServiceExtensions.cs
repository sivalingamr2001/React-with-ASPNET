using Microsoft.Extensions.DependencyInjection;
using Janatics.DataEngine.Abstractions;
using Janatics.DataEngine.Core.Auditing;
using Janatics.DataEngine.Core.Processing;
using Janatics.DataEngine.Infrastructure.Resilience;

namespace Janatics.DataEngine;

public static class DataEngineServiceExtensions
{
    public static IServiceCollection AddJanaticsDataEngine(this IServiceCollection services, Action<DataEngineOptions> configure)
    {
        // 1. Setup Options
        var options = new DataEngineOptions();
        configure(options);
        services.AddSingleton(options);

        // 2. Register Infrastructure
        services.AddSingleton<IResilientConnectionFactory, ResilientConnectionFactory>();

        // 3. Register Core Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IProcessService, ProcessService>();
        services.AddScoped<IValidationService, ValidationService>();

        // 4. Register the Main Entry Point
        services.AddScoped<DataEngine>();

        return services;
    }
}
