namespace Janatics.DataEngine.QueryEngine.Pipeline;

public interface IFetchPipeline
{
    Task<FetchPipelineResult> ExecuteAsync(FetchPipelineContext context, CancellationToken ct = default);
}
