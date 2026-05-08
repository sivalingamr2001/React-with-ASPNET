using System.Data;

namespace DataEngine.Infrastruture.ConnectionFactory;

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
