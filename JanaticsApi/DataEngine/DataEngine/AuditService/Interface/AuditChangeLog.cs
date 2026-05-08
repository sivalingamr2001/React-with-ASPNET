namespace DataEngine.AuditService.Interface;

/// <summary>
/// Represents the changelog structure stored in JSONB
/// </summary>
public class AuditChangeLog
{
    public required string ChangeUser { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Table { get; init; }
    public string? TableDisplayName { get; init; }
    public required Guid TransactionId { get; init; }
    public required string Operation { get; init; }
    public Dictionary<string, AuditFieldChange>? Changes { get; init; }
    public string? DisplayRecordName { get; init; }
}

/// <summary>
/// Represents a single field change with before/after values
/// </summary>
public class AuditFieldChange
{
    public object? Before { get; init; }
    public object? After { get; init; }
}
