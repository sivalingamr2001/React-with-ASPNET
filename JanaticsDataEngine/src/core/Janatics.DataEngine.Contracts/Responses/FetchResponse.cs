namespace Janatics.DataEngine.Contracts.Responses;

public sealed record FetchResponse
{
    public required string QueryKey { get; init; }
    public required IReadOnlyList<IDictionary<string, object?>> Data { get; init; }
    public string? TerminationReason { get; init; }
    public int Count => Data.Count;
}
