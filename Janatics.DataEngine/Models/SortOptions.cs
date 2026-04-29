namespace Janatics.DataEngine.Models;

/// <summary>
/// Represents a single sort rule.
/// </summary>
public sealed record SortOptions(
    string Column,
    bool Descending = false);
