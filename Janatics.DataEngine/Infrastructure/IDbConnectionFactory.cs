using System.Data.Common;
using Janatics.DataEngine.Registry;

namespace Janatics.DataEngine.Infrastructure;

/// <summary>
/// Creates open connections for registry and business databases.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Opens the metadata registry connection.
    /// </summary>
    Task<DbConnection> CreateOpenMetadataConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a business database connection for the provided profile.
    /// </summary>
    Task<DbConnection> CreateOpenConnectionAsync(DbProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the SQL dialect for a provider.
    /// </summary>
    SqlDialect GetDialect(string provider);
}
