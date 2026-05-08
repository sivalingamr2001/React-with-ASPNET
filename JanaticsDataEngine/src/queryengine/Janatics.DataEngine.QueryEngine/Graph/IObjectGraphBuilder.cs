using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.QueryEngine.Graph;

public interface IObjectGraphBuilder
{
    IReadOnlyList<IDictionary<string, object?>> BuildGraph(
        IEnumerable<IDictionary<string, object?>> rootRows,
        IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>> childCollections,
        QueryDefinition definition);
}
