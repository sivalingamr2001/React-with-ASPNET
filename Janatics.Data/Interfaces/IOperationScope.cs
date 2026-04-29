using System.Data;

namespace KATCRUDServices.Core.Interfaces;

/// <summary>
/// Represents a scope for database operations that shares a single connection
/// across multiple queries within the same logical operation.
/// </summary>
public interface IOperationScope : IAsyncDisposable
{
    /// <summary>
    /// Gets the shared database connection for this operation scope.
    /// </summary>
    IDbConnection Connection { get; }
    
    /// <summary>
    /// Creates a database command using the shared connection.
    /// </summary>
    /// <param name="commandText">The SQL command text</param>
    /// <returns>A database command configured with the shared connection</returns>
    Task<IDbCommand> CreateCommandAsync(string commandText);
    
    /// <summary>
    /// Indicates whether the connection is currently open and available for use.
    /// </summary>
    bool IsConnectionOpen { get; }
    
    /// <summary>
    /// Ensures the connection is open and ready for use.
    /// </summary>
    Task EnsureConnectionOpenAsync();
}