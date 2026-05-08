namespace Janatics.DataEngine.QueryEngine.Pipeline;

public interface IFetchPipelineStage
{
    int Order { get; }
    Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct);
}
