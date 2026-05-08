using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public sealed class OperationDetector : IOperationDetector
{
    public OperationType Detect(NodeProcessingContext context)
    {
        if (context.IsDeleted)
        {
            return OperationType.Delete;
        }

        return context.NodeId is null || IsEmptyIdentifier(context.NodeId)
            ? OperationType.Insert
            : OperationType.Update;
    }

    private static bool IsEmptyIdentifier(object id) => id switch
    {
        int value => value == 0,
        long value => value == 0,
        Guid value => value == Guid.Empty,
        string value => string.IsNullOrWhiteSpace(value),
        _ => false
    };
}
