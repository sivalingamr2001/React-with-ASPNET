using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IQueryPlanCache
{
    bool TryGet(string cacheKey, out CompiledQueryPlan? plan);
    void Set(CompiledQueryPlan plan, TimeSpan ttl);
}
