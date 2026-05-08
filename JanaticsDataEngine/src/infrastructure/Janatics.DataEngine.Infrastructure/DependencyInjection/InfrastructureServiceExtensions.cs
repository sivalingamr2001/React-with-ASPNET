using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.Infrastructure.Repositories;
using Janatics.DataEngine.Providers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddDataEngineInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMetadataRepository, InMemoryMetadataRepository>();
        services.AddSingleton<IConnectionStringResolver, ConfigurationConnectionStringResolver>();
        services.AddSingleton<IDbProviderFactory, DbProviderFactory>();

        services.AddSingleton<IDbProvider>(_ =>
            new RegisteredDbProvider(
                Janatics.DataEngine.Domain.Enumerations.DataProviderType.SqlServer,
                "Microsoft.Data.SqlClient",
                new SqlDialect("@"),
                new DbCapabilities
                {
                    SupportsBulkInsert = true,
                    SupportsWindowFunctions = true,
                    SupportsJsonColumns = true
                }));

        services.AddSingleton<IDbProvider>(_ =>
            new RegisteredDbProvider(
                Janatics.DataEngine.Domain.Enumerations.DataProviderType.MySql,
                "MySqlConnector",
                new SqlDialect("@", "`", "`", "SELECT LAST_INSERT_ID();"),
                new DbCapabilities
                {
                    SupportsBulkInsert = true,
                    SupportsWindowFunctions = true,
                    SupportsJsonColumns = true
                }));

        return services;
    }
}
