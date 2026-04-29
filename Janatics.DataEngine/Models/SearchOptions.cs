namespace Janatics.DataEngine.Models;

/// <summary>
/// Represents a search clause spanning multiple columns.
/// </summary>
public sealed record SearchOptions(
    string Term,
    IReadOnlyList<string>? Columns = null);
