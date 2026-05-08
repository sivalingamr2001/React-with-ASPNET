using Janatics.DataEngine.QueryEngine.Execution;
using Janatics.DataEngine.QueryEngine.Pipeline;

namespace Janatics.DataEngine.QueryEngine.Stages;

public sealed class ExecutionStage(IDbQueryExecutor queryExecutor) : IFetchPipelineStage
{
    public int Order => 40;

    public async Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        context.RootResults = await queryExecutor.ExecuteAsync(
            context.QueryDefinition!,
            context.BoundParameters ?? new Dictionary<string, object?>(),
            ct).ConfigureAwait(false);
    }
}
