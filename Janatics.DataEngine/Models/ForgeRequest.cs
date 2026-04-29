namespace Janatics.DataEngine.Models;

/// <summary>
/// Describes a dynamic write operation for a registered entity.
/// </summary>
public sealed record ForgeRequest(
    string Entity,
    string Operation,
    IReadOnlyDictionary<string, object?>? Data = null,
    object? KeyValue = null,
    IReadOnlyList<NodeForge>? Nodes = null,
    IReadOnlyList<string>? Roles = null);
