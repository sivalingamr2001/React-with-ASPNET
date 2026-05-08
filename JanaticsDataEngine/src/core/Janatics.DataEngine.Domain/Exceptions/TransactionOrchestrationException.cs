namespace Janatics.DataEngine.Domain.Exceptions;

public sealed class TransactionOrchestrationException : DataEngineException
{
    public TransactionOrchestrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
