using System.Data;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Services;

/// <summary>
/// Implementation of IOperationScope that manages a shared database connection
/// for multiple operations within a single logical scope.
/// </summary>
public class OperationScope : IOperationScope
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly DatabaseConfig _databaseConfig;
    private readonly ILogger<OperationScope> _logger;
    private IDbConnection? _connection;
    private bool _disposed = false;

    public OperationScope(
        IDatabaseConnectionFactory connectionFactory,
        DatabaseConfig databaseConfig,
        ILogger<OperationScope> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Constructor that takes a pre-created connection for enhanced connection factory usage
    /// </summary>
    public OperationScope(
        IDbConnection connection,
        ILogger<OperationScope> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = null!; // Not needed when connection is provided
        _databaseConfig = null!; // Not needed when connection is provided
    }

    /// <inheritdoc />
    public IDbConnection Connection
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OperationScope));
            
            if (_connection == null)
            {
                if (_connectionFactory == null)
                    throw new InvalidOperationException("Connection factory is not available and no connection was provided");
                    
                _connection = _connectionFactory.CreateConnection(_databaseConfig);
                _logger.LogDebug("Created new database connection for operation scope");
            }
            
            return _connection;
        }
    }

    /// <inheritdoc />
    public bool IsConnectionOpen => _connection?.State == ConnectionState.Open;

    /// <inheritdoc />
    public async Task EnsureConnectionOpenAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OperationScope));
        
        var connection = Connection; // This will create the connection if it doesn't exist
        
        if (connection.State != ConnectionState.Open)
        {
            // Use async open if available (for modern providers like Npgsql)
            if (connection is Npgsql.NpgsqlConnection npgsqlConnection)
            {
                await npgsqlConnection.OpenAsync();
                _logger.LogDebug("Opened PostgreSQL connection asynchronously");
            }
            else
            {
                // Fallback to synchronous open for other providers
                connection.Open();
                _logger.LogDebug("Opened database connection synchronously");
            }
        }
    }

    /// <inheritdoc />
    public async Task<IDbCommand> CreateCommandAsync(string commandText)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OperationScope));
        
        if (string.IsNullOrWhiteSpace(commandText))
            throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));

        await EnsureConnectionOpenAsync();
        
        var command = Connection.CreateCommand();
        command.CommandText = commandText;
        
        _logger.LogDebug("Created database command with text: {CommandText}", 
            commandText.Length > 100 ? commandText[..100] + "..." : commandText);
        
        return command;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (_connection != null)
            {
                // Use async dispose if available
                if (_connection is IAsyncDisposable asyncDisposableConnection)
                {
                    await asyncDisposableConnection.DisposeAsync();
                    _logger.LogDebug("Disposed database connection asynchronously");
                }
                else
                {
                    _connection.Dispose();
                    _logger.LogDebug("Disposed database connection synchronously");
                }
                
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while disposing operation scope connection");
            // Don't rethrow - disposal should not fail
        }
        finally
        {
            _disposed = true;
        }
    }
}