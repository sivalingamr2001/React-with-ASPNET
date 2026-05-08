using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.QueryEngine.Execution;

namespace Janatics.DataEngine.QueryEngine.Graph;

public sealed class ChildCollectionLoader(
    IDbQueryExecutor executor,
    IMetadataRepository metadataRepository) : IChildCollectionLoader
{
    public async Task<IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>>> LoadChildrenAsync(
        string tenantCode,
        QueryDefinition parentQuery,
        IEnumerable<IDictionary<string, object?>> rootRows,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var rootRowList = rootRows.ToList();

        foreach (var childDefinition in parentQuery.Children)
        {
            var childQuery = await metadataRepository.GetQueryDefinitionAsync(tenantCode, childDefinition.QueryKey, ct).ConfigureAwait(false);
            if (childQuery is null)
            {
                continue;
            }

            var parentIds = rootRowList
                .Select(x => x.TryGetValue(childDefinition.ParentKeyField, out var value) ? value : null)
                .Where(x => x is not null)
                .Distinct()
                .ToList();

            if (parentIds.Count == 0)
            {
                result[childDefinition.CollectionAlias] = [];
                continue;
            }

            var parameters = new Dictionary<string, object?>
            {
                [childDefinition.ChildForeignKeyParam] = parentIds
            };

            result[childDefinition.CollectionAlias] = await executor.ExecuteAsync(childQuery, parameters, ct).ConfigureAwait(false);
        }

        return result;
    }
}
