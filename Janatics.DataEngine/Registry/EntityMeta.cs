namespace Janatics.DataEngine.Registry;

/// <summary>
/// Describes a registry-managed entity and its allowed columns.
/// </summary>
public sealed record EntityMeta(
    Guid EntityId,
    Guid ProfileId,
    string EntityName,
    string TableName,
    string? SchemaName,
    string PrimaryKey,
    string? SoftDeleteColumn,
    bool IsReadOnly,
    IReadOnlySet<string> AllowedRoles,
    IReadOnlyDictionary<string, ColumnMeta> Columns,
    IReadOnlySet<string> RequiredColumns,
    IReadOnlySet<string> SearchableColumns)
{
    /// <summary>
    /// Gets the fully qualified table name from registry metadata.
    /// </summary>
    public string QualifiedTableName =>
        string.IsNullOrWhiteSpace(SchemaName) ? TableName : $"{SchemaName}.{TableName}";
}
