namespace DataEngine.AuditService.Interface;

/// <summary>
/// Represents an audit job to be processed asynchronously
/// </summary>
public class AuditJob
{
    public required string EntityName { get; init; }
    public required string TransactionTable { get; init; }
    public string? TableDisplayName { get; init; }
    public required Guid TransactionId { get; init; }
    public required AuditOperation Operation { get; init; }
    public required string ModifiedBy { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public Dictionary<string, object?>? BeforeData { get; init; }
    public Dictionary<string, object?>? ExtendedProperties { get; init; }
    public TableMetadata? TableMetadata { get; init; }
}
