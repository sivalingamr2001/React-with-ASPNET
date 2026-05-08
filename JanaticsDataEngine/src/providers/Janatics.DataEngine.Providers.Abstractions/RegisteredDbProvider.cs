using System.Data.Common;
using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Providers.Abstractions;

public sealed class RegisteredDbProvider : DbProviderBase
{
    public RegisteredDbProvider(
        DataProviderType providerType,
        string providerInvariantName,
        IDbDialect dialect,
        IDbCapabilities capabilities)
        : base(new GenericDbConnectionFactory(providerInvariantName))
    {
        ProviderType = providerType;
        Dialect = dialect;
        Capabilities = capabilities;
    }

    public override DataProviderType ProviderType { get; }
    public override IDbDialect Dialect { get; }
    public override IDbCapabilities Capabilities { get; }

    private sealed class GenericDbConnectionFactory(string providerInvariantName) : IDbConnectionFactory
    {
        public async Task<System.Data.IDbConnection> CreateAsync(string connectionString, CancellationToken ct = default)
        {
            var connection = CreateConnection(connectionString);
            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(ct).ConfigureAwait(false);
                return dbConnection;
            }

            connection.Open();
            return connection;
        }

        public Task<System.Data.IDbConnection> CreateReadAsync(string connectionString, CancellationToken ct = default)
            => CreateAsync(connectionString, ct);

        private System.Data.IDbConnection CreateConnection(string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(providerInvariantName);
            var connection = factory.CreateConnection()
                ?? throw new InvalidOperationException($"Unable to create connection for provider '{providerInvariantName}'.");
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}
