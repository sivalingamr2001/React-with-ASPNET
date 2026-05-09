using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Models.Metadata;

namespace Janatics.DataEngine.ProcessService.Infrastructure.Providers;

public sealed class ProviderCapabilityRegistry : IProviderCapabilityRegistry
{
    private static readonly IReadOnlyDictionary<DatabaseProvider, ProviderCapabilities> Capabilities =
        new Dictionary<DatabaseProvider, ProviderCapabilities>
        {
            [DatabaseProvider.PostgreSQL] = new()
            {
                ProviderName = "PostgreSQL",
                SupportsJson = true,
                SupportsRecursiveCte = true,
                SupportsTransactionalDdl = true,
                SupportsUpsert = true,
                ParameterPrefix = "@"
            },
            [DatabaseProvider.SqlServer] = new()
            {
                ProviderName = "SQL Server",
                SupportsJson = true,
                SupportsRecursiveCte = true,
                SupportsTransactionalDdl = false,
                SupportsUpsert = true,
                ParameterPrefix = "@"
            },
            [DatabaseProvider.MySQL] = new()
            {
                ProviderName = "MySQL",
                SupportsJson = true,
                SupportsRecursiveCte = true,
                SupportsTransactionalDdl = false,
                SupportsUpsert = true,
                ParameterPrefix = "@"
            },
            [DatabaseProvider.Oracle] = new()
            {
                ProviderName = "Oracle",
                SupportsJson = true,
                SupportsRecursiveCte = true,
                SupportsTransactionalDdl = false,
                SupportsUpsert = true,
                ParameterPrefix = ":"
            },
            [DatabaseProvider.Sqlite] = new()
            {
                ProviderName = "SQLite",
                SupportsJson = true,
                SupportsRecursiveCte = true,
                SupportsTransactionalDdl = false,
                SupportsUpsert = true,
                ParameterPrefix = "@"
            }
        };

    public ProviderCapabilities Get(DatabaseProvider provider)
        => Capabilities.TryGetValue(provider, out var capability)
            ? capability
            : throw new NotSupportedException($"No provider capability profile found for {provider}.");
}
