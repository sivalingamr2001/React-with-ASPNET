using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Registry;
using Janatics.DataEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Janatics.DataEngine.Extensions;

/// <summary>
/// Dependency injection registration helpers for DataEngine.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DataEngine services and configuration.
    /// </summary>
    public static IServiceCollection AddJanaticsDataEngine(
        this IServiceCollection services,
        Action<DataEngineOptions> configure)
    {
        services.AddOptions<DataEngineOptions>().Configure(configure);
        services.AddMemoryCache();

        services.TryAddSingleton<EntityRegistryCache>();
        services.TryAddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.TryAddSingleton<IEntityRegistryService, EntityRegistryService>();
        services.TryAddSingleton<WhitelistValidator>();
        services.TryAddScoped<IFetchService, FetchService>();
        services.TryAddScoped<IForgeService, ForgeService>();

        return services;
    }
}
