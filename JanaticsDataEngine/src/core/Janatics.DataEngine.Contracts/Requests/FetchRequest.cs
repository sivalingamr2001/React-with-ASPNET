namespace Janatics.DataEngine.Contracts.Requests;

public sealed record FetchRequest
{
    public required string TenantCode { get; init; }
    public required string QueryKey { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public PaginationOptions? Pagination { get; init; }
}

public sealed record PaginationOptions(int Offset, int PageSize);
