using System.Data;

namespace Janatics.DataEngine.Providers.Abstractions;

public abstract class DbProviderBase : IDbProvider
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbConnectionFactory _readConnectionFactory;

    protected DbProviderBase(IDbConnectionFactory connectionFactory, IDbConnectionFactory? readConnectionFactory = null)
    {
        _connectionFactory = connectionFactory;
        _readConnectionFactory = readConnectionFactory ?? connectionFactory;
    }

    public abstract Janatics.DataEngine.Domain.Enumerations.DataProviderType ProviderType { get; }
    public abstract IDbDialect Dialect { get; }
    public abstract IDbCapabilities Capabilities { get; }

    public Task<IDbConnection> OpenConnectionAsync(string connectionString, CancellationToken ct = default)
        => _connectionFactory.CreateAsync(connectionString, ct);

    public Task<IDbConnection> OpenReadConnectionAsync(string connectionString, CancellationToken ct = default)
        => _readConnectionFactory.CreateReadAsync(connectionString, ct);
}
