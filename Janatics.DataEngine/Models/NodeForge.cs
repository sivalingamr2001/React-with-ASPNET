namespace Janatics.DataEngine.Models;

/// <summary>
/// Describes a child write operation linked to a root forge request.
/// </summary>
public sealed record NodeForge(
    string Entity,
    string Operation,
    string ParentColumn,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    string? KeyColumn = null);
