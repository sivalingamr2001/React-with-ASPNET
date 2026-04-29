namespace Janatics.DataEngine.Models;

/// <summary>
/// Describes a dynamic read operation for a registered entity.
/// </summary>
public sealed record FetchRequest(
    string Entity,
    IReadOnlyList<string>? Columns = null,
    IReadOnlyList<FilterCondition>? Filters = null,
    IReadOnlyList<SortOptions>? Sort = null,
    int Page = 1,
    int PageSize = 50,
    bool IncludeCount = false,
    SearchOptions? Search = null,
    IReadOnlyList<NodeRequest>? Nodes = null,
    IReadOnlyList<string>? Roles = null,
    bool IncludeSoftDeleted = false);
