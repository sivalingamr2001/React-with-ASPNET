using Janatics.DataEngine.TransactionEngine.Commands;
using Janatics.DataEngine.Providers.Abstractions;

namespace Janatics.DataEngine.TransactionEngine.Orchestration;

public sealed class NodeProcessor(
    IOperationDetector operationDetector,
    IInsertCommandBuilder insertCommandBuilder,
    IUpdateCommandBuilder updateCommandBuilder,
    IDeleteCommandBuilder deleteCommandBuilder,
    IDbProviderFactory providerFactory) : INodeProcessor
{
    public async Task<NodeProcessingResult> ProcessAsync(NodeProcessingContext context, CancellationToken ct)
    {
        var operation = operationDetector.Detect(context);
        var provider = providerFactory.Resolve(context.EntityMetadata.ProviderType);
        var content = new Dictionary<string, object?>(context.Content, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(context.ParentKeyField) && context.ParentKeyValue is not null)
        {
            content[context.ParentKeyField] = context.ParentKeyValue;
        }

        return operation switch
        {
            Janatics.DataEngine.Domain.Enumerations.OperationType.Insert => await insertCommandBuilder.BuildAndExecuteAsync(
                context.EntityMetadata, content, provider, context.Connection, context.Transaction, ct).ConfigureAwait(false),
            Janatics.DataEngine.Domain.Enumerations.OperationType.Update => await updateCommandBuilder.BuildAndExecuteAsync(
                context.EntityMetadata, context.NodeId!, content, provider, context.Connection, context.Transaction, ct).ConfigureAwait(false),
            Janatics.DataEngine.Domain.Enumerations.OperationType.Delete => await deleteCommandBuilder.BuildAndExecuteAsync(
                context.EntityMetadata, context.NodeId!, provider, context.Connection, context.Transaction, ct).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Unsupported operation.")
        };
    }
}
