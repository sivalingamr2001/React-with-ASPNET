using Janatics.DataEngine.Contracts.Requests;
using Janatics.DataEngine.Contracts.Responses;
using Janatics.DataEngine.QueryEngine.Pipeline;

namespace Janatics.DataEngine.QueryEngine;

public sealed class DataEngineQueryService(IFetchPipeline fetchPipeline)
{
    public async Task<FetchResponse> ExecuteAsync(FetchRequest request, CancellationToken ct = default)
    {
        var result = await fetchPipeline.ExecuteAsync(
            new FetchPipelineContext
            {
                TenantCode = request.TenantCode,
                QueryKey = request.QueryKey,
                Parameters = request.Parameters,
                Pagination = request.Pagination
            },
            ct).ConfigureAwait(false);

        return new FetchResponse
        {
            QueryKey = request.QueryKey,
            Data = result.Data,
            TerminationReason = result.TerminationReason
        };
    }
}
