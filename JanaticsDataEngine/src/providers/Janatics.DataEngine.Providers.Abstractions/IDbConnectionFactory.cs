using System.Data;

namespace Janatics.DataEngine.Providers.Abstractions;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(string connectionString, CancellationToken ct = default);
    Task<IDbConnection> CreateReadAsync(string connectionString, CancellationToken ct = default);
}
