using System.Data;
using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection(DatabaseConfig config);
    
    /// <summary>
    /// Creates an operation scope that manages a shared connection for multiple operations.
    /// </summary>
    /// <param name="config">Database configuration</param>
    /// <returns>An operation scope with a shared connection</returns>
    IOperationScope CreateOperationScope(DatabaseConfig config);
}