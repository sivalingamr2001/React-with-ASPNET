namespace Janatics.DataEngine.Exceptions;

/// <summary>
/// Base exception type for DataEngine failures.
/// </summary>
public class DataEngineException : Exception
{
    public DataEngineException(string message, string errorCode, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets a stable error code for transport-layer mapping.
    /// </summary>
    public string ErrorCode { get; }
}
