using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.Providers.Abstractions;

namespace Janatics.DataEngine.Infrastructure.Repositories;

public sealed class DbProviderFactory(IEnumerable<IDbProvider> providers) : IDbProviderFactory
{
    private readonly IReadOnlyDictionary<DataProviderType, IDbProvider> _providers =
        providers.ToDictionary(x => x.ProviderType);

    public IDbProvider Resolve(DataProviderType providerType)
        => _providers.TryGetValue(providerType, out var provider)
            ? provider
            : throw new InvalidOperationException($"No database provider registered for {providerType}.");
}
