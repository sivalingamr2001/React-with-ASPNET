using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.QueryEngine.Graph;

public interface IChildCollectionLoader
{
    Task<IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>>> LoadChildrenAsync(
        string tenantCode,
        QueryDefinition parentQuery,
        IEnumerable<IDictionary<string, object?>> rootRows,
        CancellationToken ct = default);
}
