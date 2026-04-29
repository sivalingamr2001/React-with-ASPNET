using System.Data;
using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces;

/// <summary>
/// Enhanced connection factory with advanced optimization features
/// </summary>
public interface IEnhancedConnectionFactory : IDatabaseConnectionFactory
{
    /// <summary>
    /// Creates a connection asynchronously with advanced optimization features
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config);
    
    /// <summary>
    /// Creates an operation scope asynchronously for connection sharing
    /// </summary>
    Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config);
    
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