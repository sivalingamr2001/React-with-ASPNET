namespace Janatics.DataEngine.Domain.Exceptions;

public sealed class QueryExecutionException : DataEngineException
{
    public QueryExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
