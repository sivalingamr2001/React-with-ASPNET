namespace Janatics.DataEngine.FetchService.Models;

public sealed class CompiledQueryPlan
{
    public string CacheKey { get; init; } = string.Empty;
    public string SqlTemplate { get; init; } = string.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset CompiledAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
