using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Domain.Entities;

public sealed class MetadataField
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EntityId { get; init; }
    public string FieldKey { get; init; } = default!;
    public string ColumnName { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public FieldDataType DataType { get; init; }
    public int? MaxLength { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool IsNullable { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsSystemField { get; init; }
    public string? DefaultValue { get; init; }
    public int SortOrder { get; init; }
}
