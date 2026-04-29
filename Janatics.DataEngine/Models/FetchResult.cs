namespace Janatics.DataEngine.Models;

/// <summary>
/// Represents the result of a fetch operation.
/// </summary>
public sealed record FetchResult(
    bool Success,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Data,
    int? TotalCount,
    int Page,
    int PageSize,
    string Entity,
    string Provider);
