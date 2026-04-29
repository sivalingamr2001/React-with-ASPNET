namespace Janatics.DataEngine.Models;

/// <summary>
/// Represents a single filter clause in a fetch request.
/// </summary>
public sealed record FilterCondition(
    string Column,
    string Operator,
    object? Value = null,
    object? SecondValue = null,
    IReadOnlyList<object?>? Values = null);
