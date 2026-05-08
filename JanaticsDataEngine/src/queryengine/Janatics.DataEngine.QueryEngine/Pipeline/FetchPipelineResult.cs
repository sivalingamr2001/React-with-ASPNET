namespace Janatics.DataEngine.QueryEngine.Pipeline;

public sealed class FetchPipelineResult
{
    public IReadOnlyList<IDictionary<string, object?>> Data { get; set; } = [];
    public string? TerminationReason { get; set; }
}
