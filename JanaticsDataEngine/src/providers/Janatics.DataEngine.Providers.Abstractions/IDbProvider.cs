using System.Data;
using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Providers.Abstractions;

public interface IDbProvider
{
    DataProviderType ProviderType { get; }
    IDbDialect Dialect { get; }
    IDbCapabilities Capabilities { get; }
    Task<IDbConnection> OpenConnectionAsync(string connectionString, CancellationToken ct = default);
    Task<IDbConnection> OpenReadConnectionAsync(string connectionString, CancellationToken ct = default);
}
