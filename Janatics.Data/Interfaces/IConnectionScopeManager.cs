using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces
{
    /// <summary>
    /// Manages the lifecycle of connection scopes, providing factory methods
    /// for creating scopes and executing operations within scoped contexts.
    /// </summary>
    public interface IConnectionScopeManager
    {
        /// <summary>
        /// Creates a new connection scope with optional scope identification.
        /// </summary>
        /// <param name="scopeId">Optional identifier for the scope. If null, a unique ID will be generated.</param>
        /// <returns>A new connection scope instance</returns>
        IConnectionScope CreateScope(string? scopeId = null);

        /// <summary>
        /// Executes an operation within a connection scope with automatic scope management.
        /// The scope will be automatically committed on success or rolled back on failure.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The operation to execute within the scope</param>
        /// <returns>The result of the operation</returns>
        Task<T> ExecuteInScopeAsync<T>(Func<IConnectionScope, Task<T>> operation);

        /// <summary>
        /// Executes an operation within a connection scope with automatic scope management.
        /// The scope will be automatically committed on success or rolled back on failure.
        /// </summary>
        /// <param name="operation">The operation to execute within the scope</param>
        Task ExecuteInScopeAsync(Func<IConnectionScope, Task> operation);

        /// <summary>
        /// Gets metrics about connection scope usage and performance.
        /// Uses the optimized metrics from KATReaderService.Core.
        /// </summary>
        /// <returns>Connection pool metrics</returns>
        Task<ConnectionPoolMetrics> GetScopeMetricsAsync();
    }
}