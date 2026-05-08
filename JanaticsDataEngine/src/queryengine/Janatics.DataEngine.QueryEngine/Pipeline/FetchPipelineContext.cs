using Janatics.DataEngine.Contracts.Requests;
using Janatics.DataEngine.Domain.Entities;

namespace Janatics.DataEngine.QueryEngine.Pipeline;

public sealed class FetchPipelineContext
{
    public required string TenantCode { get; init; }
    public required string QueryKey { get; init; }
    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }
    public PaginationOptions? Pagination { get; init; }
    public QueryDefinition? QueryDefinition { get; set; }
    public string? ResolvedSql { get; set; }
    public IReadOnlyDictionary<string, object?>? BoundParameters { get; set; }
    public IEnumerable<IDictionary<string, object?>>? RootResults { get; set; }
    public FetchPipelineResult Result { get; } = new();
    public bool IsTerminated { get; private set; }

    public void Terminate(string reason)
    {
        IsTerminated = true;
        Result.TerminationReason = reason;
    }
}
