namespace Janatics.DataEngine.Models;

/// <summary>
/// Describes a child fetch operation linked to a root fetch.
/// </summary>
public sealed record NodeRequest(
    string Entity,
    string ParentColumn,
    string? Alias = null,
    IReadOnlyList<string>? Columns = null,
    IReadOnlyList<FilterCondition>? Filters = null,
    IReadOnlyList<SortOptions>? Sort = null,
    SearchOptions? Search = null,
    int? Limit = null);
