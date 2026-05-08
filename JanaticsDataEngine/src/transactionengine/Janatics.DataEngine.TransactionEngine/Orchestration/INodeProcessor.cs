namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public interface INodeProcessor
{
    Task<NodeProcessingResult> ProcessAsync(NodeProcessingContext context, CancellationToken ct);
}
