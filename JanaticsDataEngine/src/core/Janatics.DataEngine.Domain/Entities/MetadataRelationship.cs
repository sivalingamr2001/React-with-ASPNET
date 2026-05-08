using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Domain.Entities;

public sealed class MetadataRelationship
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ParentEntityId { get; init; }
    public Guid ChildEntityId { get; init; }
    public string RelationshipKey { get; init; } = default!;
    public string ParentKeyField { get; init; } = default!;
    public string ChildForeignKeyField { get; init; } = default!;
    public RelationshipCardinality Cardinality { get; init; }
    public bool CascadeDelete { get; init; }
    public bool IsLazyLoadEnabled { get; init; }
    public string? ChildCollectionAlias { get; init; }
}
