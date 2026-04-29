using System.Data;

namespace KATCRUDServices.Core.Interfaces
{
    /// <summary>
    /// Represents a connection scope that manages shared database connections and transactions
    /// within a logical boundary, ensuring proper resource lifecycle management.
    /// </summary>
    public interface IConnectionScope : IDisposable
    {
        /// <summary>
        /// Gets the shared database connection for this scope.
        /// Returns the same connection instance for all calls within the scope.
        /// </summary>
        /// <returns>The shared database connection</returns>
        Task<IDbConnection> GetConnectionAsync();

        /// <summary>
        /// Gets the shared database transaction for this scope.
        /// Returns the same transaction instance for all calls within the scope.
        /// </summary>
        /// <returns>The shared database transaction</returns>
        Task<IDbTransaction> GetTransactionAsync();

        /// <summary>
        /// Commits the transaction and completes the scope successfully.
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rolls back the transaction and completes the scope with failure.
        /// </summary>
        Task RollbackAsync();

        /// <summary>
        /// Gets a value indicating whether the scope is still active and can be used.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the unique identifier for this connection scope.
        /// </summary>
        string ScopeId { get; }
    }
}