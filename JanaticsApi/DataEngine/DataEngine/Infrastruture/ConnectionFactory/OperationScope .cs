using DataEngine.Infrastruture.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace DataEngine.Infrastruture.ConnectionFactory;

public class OperationScope : IOperationScope
{
    private readonly IDatabaseConnectionFactory? _connectionFactory;
    private readonly DatabaseConfig? _databaseConfig;
    private readonly ILogger<OperationScope> _logger;
    private IDbConnection? _connection;
    private bool _disposed = false;

    // Standard Factory Constructor
    public OperationScope(IDatabaseConnectionFactory factory, DatabaseConfig config, ILogger<OperationScope> logger)
    {
        _connectionFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        _databaseConfig = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Resilient/Pre-tracked Connection Constructor
    public OperationScope(IDbConnection connection, ILogger<OperationScope> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDbConnection Connection
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_connection == null)
            {
                if (_connectionFactory == null || _databaseConfig == null)
                    throw new InvalidOperationException("No connection source available for this scope.");

                _connection = _connectionFactory.CreateConnection(_databaseConfig);
            }
            return _connection;
        }
    }

    public bool IsConnectionOpen => _connection?.State == ConnectionState.Open;

    public async Task EnsureConnectionOpenAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnectionOpen) return;

        var conn = Connection;
        if (conn is DbConnection dbConn)
        {
            await dbConn.OpenAsync();
        }
        else
        {
            conn.Open();
        }

        _logger.LogDebug("Scope connection opened successfully.");
    }

    public async Task<IDbCommand> CreateCommandAsync(string commandText)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureConnectionOpenAsync();

        var command = Connection.CreateCommand();
        command.CommandText = commandText;

        _logger.LogTrace("Created command for scope: {CommandText}", commandText);
        return command;
    }

    /// <summary>
    /// Helper to start a transaction within this shared scope
    /// </summary>
    public async Task<IDbTransaction> BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted)
    {
        await EnsureConnectionOpenAsync();
        return Connection.BeginTransaction(level);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_connection != null)
            {
                if (_connection is DbConnection dbConn)
                    await dbConn.DisposeAsync();
                else
                    _connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing OperationScope.");
        }
        finally
        {
            _disposed = true;
            _connection = null;
            GC.SuppressFinalize(this);
        }
    }
}
