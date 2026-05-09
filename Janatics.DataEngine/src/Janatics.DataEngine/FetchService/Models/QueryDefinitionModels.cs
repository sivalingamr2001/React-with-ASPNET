namespace Janatics.DataEngine.FetchService.Models;

public class SaveQueryDefinitionRequest
{
    public long? QueryNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? QueryText { get; set; }
    public string? FetchJson { get; set; }
    public bool IsFetchJson { get; set; }
    public IReadOnlyList<string> Tables { get; set; } = Array.Empty<string>();
    public string? ParameterDefinitionJson { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExecuteStoredQueryRequest : FetchExecutionRequest
{
}

public class QueryDefinitionModel
{
    public long QueryNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? QueryText { get; set; }
    public string? FetchJson { get; set; }
    public bool IsFetchJson { get; set; }
    public string? ParameterDefinitionJson { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Tables { get; set; } = Array.Empty<string>();
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public int VersionNo { get; set; }
}

public class FetchExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long QueryNumber { get; set; }
    public IReadOnlyList<Dictionary<string, object?>> Records { get; set; } = Array.Empty<Dictionary<string, object?>>();
    public int RecordCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class TableMetadataModel
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<ColumnMetadataModel> Columns { get; set; } = Array.Empty<ColumnMetadataModel>();
}

public class ColumnMetadataModel
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
    public int? OrdinalPosition { get; set; }
}
