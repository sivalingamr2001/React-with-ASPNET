namespace Janatics.DataEngine.Exceptions;

/// <summary>
/// Thrown when a write request targets a read-only entity or column.
/// </summary>
public sealed class ReadOnlyViolationException : DataEngineException
{
    public ReadOnlyViolationException(string resourceName)
        : base($"'{resourceName}' is marked as read-only in the DataEngine registry.", "READ_ONLY_VIOLATION")
    {
    }
}
