using System.Data;
using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces
{
    /// <summary>
    /// Interface for connection multiplexing to share connections across multiple CRUD operations
    /// </summary>
    public interface IConnectionMultiplexer : IAsyncDisposable
    {
        /// <summary>
        /// Executes a CRUD operation using a shared connection
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="config">Database configuration</param>
        /// <param name="operation">The operation to execute</param>
        /// <param name="useReadReplica">Whether to use read replica for read operations</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteMultiplexedAsync<T>(
            DatabaseConfig config,
            Func<IDbConnection, Task<T>> operation,
            bool useReadReplica = true);
        
        /// <summary>
        /// Executes multiple CRUD operations in parallel using connection multiplexing
        /// </summary>
        /// <typeparam name="T">Return type of the operations</typeparam>
        /// <param name="config">Database configuration</param>
        /// <param name="operations">The operations to execute</param>
        /// <param name="useReadReplica">Whether to use read replica for read operations</param>
        /// <param name="maxConcurrency">Maximum number of concurrent operations</param>
        /// <returns>Results of all operations</returns>
        Task<IEnumerable<T>> ExecuteParallelAsync<T>(
            DatabaseConfig config,
            IEnumerable<Func<IDbConnection, Task<T>>> operations,
            bool useReadReplica = true,
            int maxConcurrency = 4);
        
        /// <summary>
        /// Gets the current multiplexed connection if available
        /// </summary>
        /// <returns>Current connection or null if none available</returns>
        IDbConnection? GetCurrentConnection();
        
        /// <summary>
        /// Gets metrics about multiplexed operations
        /// </summary>
        /// <returns>Connection multiplexer metrics</returns>
        ConnectionMultiplexerMetrics GetMetrics();
    }
}