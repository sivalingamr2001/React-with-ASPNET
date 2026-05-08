namespace Janatics.DataEngine.Domain.Exceptions;

public class DataEngineException : Exception
{
    public DataEngineException(string message)
        : base(message)
    {
    }

    public DataEngineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
