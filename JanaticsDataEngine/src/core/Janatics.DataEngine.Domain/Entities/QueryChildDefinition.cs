namespace Janatics.DataEngine.Domain.Entities;

public sealed class QueryChildDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string QueryKey { get; init; } = default!;
    public string CollectionAlias { get; init; } = default!;
    public string ParentKeyField { get; init; } = default!;
    public string ChildForeignKeyField { get; init; } = default!;
    public string ChildForeignKeyParam { get; init; } = default!;
}
