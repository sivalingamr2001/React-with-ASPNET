namespace Janatics.DataEngine.Contracts.Requests;

public sealed record TransactionRequest
{
    public required string TenantCode { get; init; }
    public required string RootEntity { get; init; }
    public object? RootId { get; init; }
    public required IDictionary<string, object?> Content { get; init; }
    public IReadOnlyList<TransactionNodeRequest> Nodes { get; init; } = [];
    public string? CorrelationId { get; init; }
    public string? InitiatedBy { get; init; }
}

public sealed record TransactionNodeRequest
{
    public required string NodeEntity { get; init; }
    public required string ParentLink { get; init; }
    public object? NodeId { get; init; }
    public bool IsDeleted { get; init; }
    public required IDictionary<string, object?> Content { get; init; }
    public IReadOnlyList<TransactionNodeRequest> Nodes { get; init; } = [];
}
