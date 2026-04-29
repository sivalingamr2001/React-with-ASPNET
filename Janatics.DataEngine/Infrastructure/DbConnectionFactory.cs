using System.Data.Common;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Registry;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine.Infrastructure;

/// <summary>
/// Creates database connections for metadata and target business databases.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IOptions<DataEngineOptions> _options;

    public DbConnectionFactory(IOptions<DataEngineOptions> options)
    {
        _options = options;
    }

    public Task<DbConnection> CreateOpenMetadataConnectionAsync(CancellationToken cancellationToken = default) =>
        CreateOpenConnectionInternalAsync(_options.Value.MetadataProvider, _options.Value.MetadataConnectionString, cancellationToken);

    public Task<DbConnection> CreateOpenConnectionAsync(DbProfile profile, CancellationToken cancellationToken = default) =>
        CreateOpenConnectionInternalAsync(profile.Provider, profile.ConnectionString, cancellationToken);

    public SqlDialect GetDialect(string provider) => SqlDialect.ForProvider(provider);

    private static async Task<DbConnection> CreateOpenConnectionInternalAsync(
        string provider,
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DataEngineException("Connection string is required.", "CONNECTION_STRING_REQUIRED");
        }

        DbConnection connection = ProviderDetector.Normalize(provider) switch
        {
            "sqlite" => new SqliteConnection(connectionString),
            "sqlserver" => throw new DataEngineException("SQL Server provider wiring is reserved for a future package-enabled build.", "PROVIDER_NOT_AVAILABLE"),
            "oracle" => throw new DataEngineException("Oracle provider wiring is reserved for a future package-enabled build.", "PROVIDER_NOT_AVAILABLE"),
            _ => throw new DataEngineException($"Provider '{provider}' is not supported.", "PROVIDER_NOT_SUPPORTED")
        };

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
