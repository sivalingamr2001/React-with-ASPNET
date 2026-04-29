namespace Janatics.DataEngine.Models;

/// <summary>
/// Represents the result of a forge operation.
/// </summary>
public sealed record ForgeResult(
    bool Success,
    object? RootId,
    string Operation,
    int AffectedNodes,
    string Entity,
    string Provider);
