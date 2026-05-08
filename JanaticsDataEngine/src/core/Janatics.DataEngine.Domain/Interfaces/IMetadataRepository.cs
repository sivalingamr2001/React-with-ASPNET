using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.Domain.Interfaces;

public interface IMetadataRepository
{
    Task<MetadataEntity?> GetEntityAsync(string tenantCode, string entityKey, CancellationToken ct = default);
    Task<IReadOnlyList<MetadataEntity>> GetAllEntitiesAsync(string tenantCode, CancellationToken ct = default);
    Task<QueryDefinition?> GetQueryDefinitionAsync(string tenantCode, string queryKey, CancellationToken ct = default);
    Task<IReadOnlyList<QueryDefinition>> GetQueryDefinitionsAsync(string tenantCode, CancellationToken ct = default);
    Task UpsertEntityAsync(MetadataEntity entity, CancellationToken ct = default);
    Task UpsertQueryDefinitionAsync(QueryDefinition definition, CancellationToken ct = default);
}
