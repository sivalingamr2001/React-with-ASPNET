using System.Data;
using Janatics.DataEngine.Infrastructure.Models;

namespace Janatics.DataEngine.Abstractions;

/// <summary>
/// Resilient connection factory with advanced optimization features
/// </summary>
public interface IResilientConnectionFactory : IDatabaseConnectionFactory
{
    /// <summary>
    /// Creates a connection asynchronously with advanced optimization features
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config);
    
    /// <summary>
    /// Creates an operation scope asynchronously for connection sharing
    /// </summary>
    Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config);

    Task<IOperationScope> CreateOperationScopeAsync();

    Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation);

    Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation);

    Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null);
    Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null);
    Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null);
    Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction? transaction = null);

    Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);
    Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);
    Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);
    Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null);
    
    /// <summary>
    /// Gets current connection pool metrics
    /// </summary>
    ConnectionPoolMetrics GetPoolMetrics();
    
    /// <summary>
    /// Validates connection health asynchronously
    /// </summary>
    Task<bool> ValidateConnectionHealthAsync(IDbConnection connection);
    
    /// <summary>
    /// Creates a read-only connection for read operations (may use read replica)
    /// </summary>
    Task<IDbConnection> CreateReadOnlyConnectionAsync(DatabaseConfig config, ReadReplicaConfig? readReplicaConfig = null);
    
    /// <summary>
    /// Gets connection pool analytics and recommendations
    /// </summary>
    Task<ConnectionPoolAnalytics> GetPoolAnalyticsAsync();
    
    /// <summary>
    /// Records performance metrics for a connection operation
    /// </summary>
    Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics);
    
    /// <summary>
    /// Gets connection pool health status
    /// </summary>
    Task<ConnectionPoolHealth> GetPoolHealthAsync();
}