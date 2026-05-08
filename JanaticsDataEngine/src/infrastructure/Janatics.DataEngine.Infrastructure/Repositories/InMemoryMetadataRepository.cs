using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Interfaces;

namespace Janatics.DataEngine.Infrastructure.Repositories;

public sealed class InMemoryMetadataRepository : IMetadataRepository
{
    private readonly Dictionary<string, MetadataEntity> _entities = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, QueryDefinition> _queries = new(StringComparer.OrdinalIgnoreCase);

    public Task<MetadataEntity?> GetEntityAsync(string tenantCode, string entityKey, CancellationToken ct = default)
    {
        _entities.TryGetValue(Key(tenantCode, entityKey), out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<MetadataEntity>> GetAllEntitiesAsync(string tenantCode, CancellationToken ct = default)
    {
        var result = _entities.Values.Where(x => x.TenantCode.Equals(tenantCode, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<MetadataEntity>>(result);
    }

    public Task<QueryDefinition?> GetQueryDefinitionAsync(string tenantCode, string queryKey, CancellationToken ct = default)
    {
        _queries.TryGetValue(Key(tenantCode, queryKey), out var query);
        return Task.FromResult(query);
    }

    public Task<IReadOnlyList<QueryDefinition>> GetQueryDefinitionsAsync(string tenantCode, CancellationToken ct = default)
    {
        var result = _queries.Values.Where(x => x.TenantCode.Equals(tenantCode, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<QueryDefinition>>(result);
    }

    public Task UpsertEntityAsync(MetadataEntity entity, CancellationToken ct = default)
    {
        _entities[Key(entity.TenantCode, entity.EntityKey)] = entity;
        return Task.CompletedTask;
    }

    public Task UpsertQueryDefinitionAsync(QueryDefinition definition, CancellationToken ct = default)
    {
        _queries[Key(definition.TenantCode, definition.QueryKey)] = definition;
        return Task.CompletedTask;
    }

    private static string Key(string tenantCode, string key) => $"{tenantCode}::{key}";
}
