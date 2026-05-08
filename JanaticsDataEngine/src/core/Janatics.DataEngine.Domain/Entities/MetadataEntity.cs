using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Domain.Entities;

public sealed class MetadataEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantCode { get; init; } = default!;
    public string EntityKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public string? SchemaName { get; init; }
    public string PrimaryKeyField { get; init; } = default!;
    public bool IsIdentityPk { get; init; }
    public string? ConcurrencyField { get; init; }
    public string? SoftDeleteField { get; init; }
    public string? CreatedAtField { get; init; }
    public string? UpdatedAtField { get; init; }
    public string? CreatedByField { get; init; }
    public string? UpdatedByField { get; init; }
    public bool IsAuditEnabled { get; init; }
    public DataProviderType ProviderType { get; init; }
    public string ConnectionName { get; init; } = default!;
    public IReadOnlyList<MetadataField> Fields { get; init; } = [];
    public IReadOnlyList<MetadataRelationship> Relationships { get; init; } = [];
}
