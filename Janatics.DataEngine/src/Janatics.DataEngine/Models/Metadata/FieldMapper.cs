namespace Janatics.DataEngine.Models.Metadata;

public class FieldMapperModel
{
    public string EntityName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public string DataType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Properties { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? SequenceName { get; set; }
    public bool AllowUpdate { get; set; } = true;
}