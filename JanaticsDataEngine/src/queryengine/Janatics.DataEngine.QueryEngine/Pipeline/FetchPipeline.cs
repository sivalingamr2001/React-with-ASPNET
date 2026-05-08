using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.QueryEngine.Pipeline;

public sealed class FetchPipeline(IEnumerable<IFetchPipelineStage> stages, ILogger<FetchPipeline> logger) : IFetchPipeline
{
    private readonly IReadOnlyList<IFetchPipelineStage> _stages = stages.OrderBy(x => x.Order).ToList();

    public async Task<FetchPipelineResult> ExecuteAsync(FetchPipelineContext context, CancellationToken ct = default)
    {
        foreach (var stage in _stages)
        {
            logger.LogDebug("Executing stage {Stage} for query {QueryKey}.", stage.GetType().Name, context.QueryKey);
            await stage.ExecuteAsync(context, ct).ConfigureAwait(false);

            if (context.IsTerminated)
            {
                break;
            }
        }

        return context.Result;
    }
}
