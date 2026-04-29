using System.Data;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Repositories;

/// <summary>
/// Base repository class that provides connection sharing support and common database operations
/// for all repositories in the CRUD services
/// </summary>
public abstract class BaseRepository
{
    protected readonly IEnhancedDataProvider _dataProvider;
    protected readonly ILogger _logger;

    protected BaseRepository(IEnhancedDataProvider dataProvider, ILogger logger)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with optional connection parameter support.
    /// If connection is provided, uses it; otherwise creates a new connection scope.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="connection">Optional connection to use</param>
    /// <returns>Result of the operation</returns>
    protected async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> operation, IDbConnection? connection = null)
    {
        if (connection != null)
        {
            // Use provided connection
            _logger.LogDebug("Using provided connection for repository operation");
            return await operation(connection);
        }
        else
        {
            // Create new connection scope
            _logger.LogDebug("Creating new connection scope for repository operation");
            return await _dataProvider.ExecuteInScopeAsync(operation);
        }
    }

    /// <summary>
    /// Executes an operation within a transaction with optional connection/transaction parameter support.
    /// If connection and transaction are provided, uses them; otherwise creates a new transaction scope.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="connection">Optional connection to use</param>
    /// <param name="transaction">Optional transaction to use</param>
    /// <returns>Result of the operation</returns>
    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null)
    {
        if (connection != null && transaction != null)
        {
            // Use provided connection and transaction
            _logger.LogDebug("Using provided connection and transaction for repository operation");
            return await operation(connection, transaction);
        }
        else
        {
            // Create new transaction scope
            _logger.LogDebug("Creating new transaction scope for repository operation");
            return await _dataProvider.ExecuteInTransactionScopeAsync(operation);
        }
    }

    /// <summary>
    /// Executes a non-query SQL statement with connection-aware support
    /// </summary>
    /// <param name="sql">SQL statement to execute</param>
    /// <param name="parameters">Parameters for the SQL statement</param>
    /// <param name="connection">Optional connection to use</param>
    /// <param name="transaction">Optional transaction to use</param>
    /// <returns>Number of rows affected</returns>
    protected async Task<int> ExecuteNonQueryAsync(
        string sql, 
        Dictionary<string, object> parameters, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null)
    {
        if (connection != null)
        {
            return await _dataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);
        }
        else
        {
            return await _dataProvider.ExecuteNonQueryAsync(sql, parameters, transaction!);
        }
    }

    /// <summary>
    /// Executes a scalar SQL statement with connection-aware support
    /// </summary>
    /// <param name="sql">SQL statement to execute</param>
    /// <param name="parameters">Parameters for the SQL statement</param>
    /// <param name="connection">Optional connection to use</param>
    /// <param name="transaction">Optional transaction to use</param>
    /// <returns>Scalar result</returns>
    protected async Task<object?> ExecuteScalarAsync(
        string sql, 
        Dictionary<string, object> parameters, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null)
    {
        if (connection != null)
        {
            return await _dataProvider.ExecuteScalarAsync(sql, parameters, connection, transaction);
        }
        else
        {
            return await _dataProvider.ExecuteScalarAsync(sql, parameters, transaction!);
        }
    }

    /// <summary>
    /// Executes a query SQL statement with connection-aware support
    /// </summary>
    /// <param name="sql">SQL statement to execute</param>
    /// <param name="parameters">Parameters for the SQL statement</param>
    /// <param name="connection">Optional connection to use</param>
    /// <param name="transaction">Optional transaction to use</param>
    /// <returns>DataTable with query results</returns>
    protected async Task<DataTable> ExecuteQueryAsync(
        string sql, 
        Dictionary<string, object> parameters, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null)
    {
        if (connection != null)
        {
            return await _dataProvider.ExecuteQueryAsync(sql, parameters, connection, transaction);
        }
        else
        {
            return await _dataProvider.ExecuteQueryAsync(sql, parameters, transaction);
        }
    }

    /// <summary>
    /// Executes a bulk insert operation with connection-aware support
    /// </summary>
    /// <param name="tableName">Name of the table to insert into</param>
    /// <param name="dataTable">Data to insert</param>
    /// <param name="connection">Optional connection to use</param>
    /// <param name="transaction">Optional transaction to use</param>
    /// <returns>Number of rows inserted</returns>
    protected async Task<int> BulkInsertAsync(
        string tableName, 
        DataTable dataTable, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null)
    {
        if (connection != null)
        {
            return await _dataProvider.BulkInsertAsync(tableName, dataTable, connection, transaction);
        }
        else
        {
            return await _dataProvider.BulkInsertAsync(tableName, dataTable, transaction!);
        }
    }

    /// <summary>
    /// Validates connection health
    /// </summary>
    /// <param name="connection">Connection to validate</param>
    /// <returns>True if connection is healthy</returns>
    protected async Task<bool> ValidateConnectionHealthAsync(IDbConnection connection)
    {
        return await _dataProvider.ValidateConnectionHealthAsync(connection);
    }

    /// <summary>
    /// Gets connection pool metrics
    /// </summary>
    /// <returns>Current connection pool metrics</returns>
    protected ConnectionPoolMetrics GetPoolMetrics()
    {
        return _dataProvider.GetPoolMetrics();
    }

    /// <summary>
    /// Creates an operation scope for sharing connections across multiple operations
    /// </summary>
    /// <returns>Operation scope with shared connection</returns>
    protected async Task<IOperationScope> CreateOperationScopeAsync()
    {
        return await _dataProvider.CreateOperationScopeAsync();
    }
}