namespace Janatics.DataEngine.ProcessService.Model;

public class TableAuditMetadata
{
    public required string TableName { get; init; }
    public bool AuditEnabled { get; init; }
}
