using System.Text.Json.Serialization;

namespace Janatics.DataEngine.FetchService.Models;

public class FetchExecutionRequest
{
    public long QueryNumber { get; set; }
    public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? QueryText { get; set; }
    public bool EnableDirectQueryExecution { get; set; }
    public string? ExecutedBy { get; set; }
}

public class FetchAuditLogEntry
{
    public long QueryNumber { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string? ExecutedQuery { get; set; }
    public string? ParametersJson { get; set; }
    public int? ResultCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? CreatedBy { get; set; }
}

public class GeneratedFetchCommand
{
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class FetchJsonQuery
{
    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();

    [JsonPropertyName("joins")]
    public List<FetchJsonJoin> Joins { get; set; } = new();

    [JsonPropertyName("filter")]
    public FetchJsonFilter? Filter { get; set; }

    [JsonPropertyName("orderBy")]
    public List<FetchJsonOrderBy> OrderBy { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 50;
}

public class FetchJsonJoin
{
    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "inner";

    [JsonPropertyName("isIntersect")]
    public bool IsIntersect { get; set; }

    [JsonPropertyName("from")]
    public FetchJsonJoinKey From { get; set; } = new();

    [JsonPropertyName("toField")]
    public string ToField { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();
}

public class FetchJsonJoinKey
{
    [JsonPropertyName("entityAlias")]
    public string EntityAlias { get; set; } = string.Empty;

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
}

public class FetchJsonFilter
{
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "and";

    [JsonPropertyName("conditions")]
    public List<FetchJsonCondition>? Conditions { get; set; }

    [JsonPropertyName("groups")]
    public List<FetchJsonFilter>? Groups { get; set; }
}

public class FetchJsonCondition
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "eq";

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

public class FetchJsonOrderBy
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "asc";
}
