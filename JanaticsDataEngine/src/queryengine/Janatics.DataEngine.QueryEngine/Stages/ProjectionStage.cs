using Janatics.DataEngine.QueryEngine.Graph;
using Janatics.DataEngine.QueryEngine.Pipeline;

namespace Janatics.DataEngine.QueryEngine.Stages;

public sealed class ProjectionStage(
    IChildCollectionLoader childCollectionLoader,
    IObjectGraphBuilder objectGraphBuilder) : IFetchPipelineStage
{
    public int Order => 50;

    public async Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        var rootRows = context.RootResults?.ToList() ?? [];
        var childCollections = await childCollectionLoader.LoadChildrenAsync(
            context.TenantCode,
            context.QueryDefinition!,
            rootRows,
            ct).ConfigureAwait(false);

        context.Result.Data = objectGraphBuilder.BuildGraph(rootRows, childCollections, context.QueryDefinition!);
    }
}
