using Janatics.DataEngine.ProcessService.Infrastructure.Models;

namespace Janatics.DataEngine.FetchService.Models;

public class FetchServiceOptions
{
    public DatabaseConfig DatabaseConfig { get; set; } = new();
    public string SchemaName { get; set; } = "public";
    public string QueryDefinitionTableName { get; set; } = "fetchquerydefinition";
    public string QueryTableMapTableName { get; set; } = "fetchquerytablemap";
    public string QueryAuditLogTableName { get; set; } = "fetchqueryauditlog";
    public bool AllowDirectQueryExecution { get; set; } = true;
    public int MaxDirectQueryLength { get; set; } = 10000;
    public int DefaultPageSize { get; set; } = 100;
    public int MaxPageSize { get; set; } = 1000;
}
