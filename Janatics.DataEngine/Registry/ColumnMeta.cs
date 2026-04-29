namespace Janatics.DataEngine.Registry;

/// <summary>
/// Describes a registered entity column.
/// </summary>
public sealed record ColumnMeta(
    Guid ColumnId,
    Guid EntityId,
    string ColumnName,
    string DataType,
    bool IsRequired,
    bool IsPrimaryKey,
    bool IsSearchable,
    bool IsReadOnly,
    bool IsNullable,
    int OrdinalPosition);
