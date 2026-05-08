using DataEngine.Infrastruture.ConnectionFactory;
using DataEngine.Infrastruture.Models;
using Janatics.DataEngine.Core.Auditing;
using Janatics.DataEngine.Infrastructure.Models;
using System.Data;

namespace Janatics.DataEngine.Abstractions;

/// <summary>
/// Enhanced data provider interface that extends IDataProvider with connection-aware operations
/// and advanced connection management features for optimal resource utilization
/// </summary>
public interface IResilientDataProvider : IDataProvider
{
    // Connection scope operations
    Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation);
    Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation);

    // Connection-aware operations
    Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);
    Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);
    Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null);

    // Bulk operations with shared connections
    Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null);

    // Connection health and monitoring
    Task<bool> ValidateConnectionHealthAsync(IDbConnection connection);
    ConnectionPoolMetrics GetPoolMetrics();

    /// <summary>
    /// Creates an operation scope for sharing connections across multiple operations
    /// </summary>
    Task<IOperationScope> CreateOperationScopeAsync();

    /// <summary>
    /// Gets connection pool health status
    /// </summary>
    Task<ConnectionPoolHealth> GetPoolHealthAsync();

    /// <summary>
    /// Records performance metrics for connection operations
    /// </summary>
    Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics);
}