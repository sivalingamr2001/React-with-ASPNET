using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.QueryEngine.Graph;

public sealed class ObjectGraphBuilder : IObjectGraphBuilder
{
    public IReadOnlyList<IDictionary<string, object?>> BuildGraph(
        IEnumerable<IDictionary<string, object?>> rootRows,
        IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>> childCollections,
        QueryDefinition definition)
    {
        var rootList = rootRows.Select(CloneRow).ToList();

        foreach (var childDefinition in definition.Children)
        {
            if (!childCollections.TryGetValue(childDefinition.CollectionAlias, out var childRows))
            {
                continue;
            }

            var groupedRows = childRows
                .GroupBy(x => x.TryGetValue(childDefinition.ChildForeignKeyField, out var value) ? value : null)
                .Where(x => x.Key is not null)
                .ToDictionary(x => x.Key!, x => x.Select(CloneRow).ToList());

            foreach (var row in rootList)
            {
                if (!row.TryGetValue(childDefinition.ParentKeyField, out var key) || key is null)
                {
                    row[childDefinition.CollectionAlias] = new List<IDictionary<string, object?>>();
                    continue;
                }

                row[childDefinition.CollectionAlias] = groupedRows.TryGetValue(key, out var children)
                    ? children
                    : new List<IDictionary<string, object?>>();
            }
        }

        return rootList;
    }

    private static IDictionary<string, object?> CloneRow(IDictionary<string, object?> source)
        => new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);
}
