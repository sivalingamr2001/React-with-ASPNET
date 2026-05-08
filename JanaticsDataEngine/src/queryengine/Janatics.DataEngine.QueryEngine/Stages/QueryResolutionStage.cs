using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.QueryEngine.Pipeline;

namespace Janatics.DataEngine.QueryEngine.Stages;

public sealed class QueryResolutionStage(IMetadataRepository metadataRepository) : IFetchPipelineStage
{
    public int Order => 10;

    public async Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        var definition = await metadataRepository.GetQueryDefinitionAsync(context.TenantCode, context.QueryKey, ct).ConfigureAwait(false);
        if (definition is null)
        {
            context.Terminate($"Query definition not found: {context.QueryKey}");
            return;
        }

        if (!definition.IsActive)
        {
            context.Terminate($"Query definition is inactive: {context.QueryKey}");
            return;
        }

        context.QueryDefinition = definition;
    }
}
