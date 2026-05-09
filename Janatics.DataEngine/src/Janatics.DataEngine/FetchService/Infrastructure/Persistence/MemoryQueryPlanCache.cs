using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Janatics.DataEngine.FetchService.Infrastructure.Persistence;

public sealed class MemoryQueryPlanCache(IMemoryCache memoryCache) : IQueryPlanCache
{
    private readonly IMemoryCache _memoryCache = memoryCache;

    public bool TryGet(string cacheKey, out CompiledQueryPlan? plan)
    {
        var found = _memoryCache.TryGetValue(cacheKey, out var cached);
        plan = found ? cached as CompiledQueryPlan : null;
        return found && plan is not null;
    }

    public void Set(CompiledQueryPlan plan, TimeSpan ttl)
    {
        _memoryCache.Set(plan.CacheKey, plan, ttl);
    }
}
