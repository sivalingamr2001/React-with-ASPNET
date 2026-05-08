using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Providers.Abstractions;

public interface IDbProviderFactory
{
    IDbProvider Resolve(DataProviderType providerType);
}
