using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;

namespace Janatics.DataEngine;

public class DataEngineOptions
{
    public bool EnableResilience { get; set; } = true;
    public bool EnableAudit { get; set; } = true;
    public bool RequireMasterTables { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public int OperationTimeoutSeconds { get; set; } = 30;
    public int MaxProcessDepth { get; set; } = 5;
    public int MaxChildNodesPerRequest { get; set; } = 500;
    public string LogsFolderPath { get; set; } = "logs";
    public DatabaseConfig DatabaseConfig { get; set; } = new();
    public FetchServiceOptions FetchService { get; set; } = new();
}
