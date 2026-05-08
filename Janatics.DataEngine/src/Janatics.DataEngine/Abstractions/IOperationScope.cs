using System.Data;

namespace Janatics.DataEngine.Abstractions;

public interface IOperationScope : IAsyncDisposable
{
    IDbConnection Connection { get; }

    bool IsConnectionOpen { get; }

    Task EnsureConnectionOpenAsync();

    Task<IDbCommand> CreateCommandAsync(string commandText);

    /// <summary>
    /// Starts a transaction on the shared connection.
    /// </summary>
    Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
