using System.Data;
using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public sealed class NodeProcessingContext
{
    public required string TenantCode { get; init; }
    public required MetadataEntity EntityMetadata { get; init; }
    public object? NodeId { get; init; }
    public required IDictionary<string, object?> Content { get; init; }
    public bool IsDeleted { get; init; }
    public string? ParentKeyField { get; init; }
    public object? ParentKeyValue { get; init; }
    public required IDbConnection Connection { get; init; }
    public required IDbTransaction Transaction { get; init; }
}
