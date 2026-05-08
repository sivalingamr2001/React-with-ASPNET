using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public interface IOperationDetector
{
    OperationType Detect(NodeProcessingContext context);
}
