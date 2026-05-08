using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.QueryEngine.Execution;

public interface IDbQueryExecutor
{
    Task<IReadOnlyList<IDictionary<string, object?>>> ExecuteAsync(
        QueryDefinition definition,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default);
}
