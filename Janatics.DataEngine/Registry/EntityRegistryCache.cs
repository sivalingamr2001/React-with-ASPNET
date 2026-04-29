using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine.Registry;

/// <summary>
/// Stores registry entities and profiles in memory.
/// </summary>
public sealed class EntityRegistryCache
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<DataEngineOptions> _options;

    public EntityRegistryCache(IMemoryCache cache, IOptions<DataEngineOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    public bool TryGetEntity(string entityName, out EntityMeta? entityMeta) =>
        _cache.TryGetValue(GetEntityKey(entityName), out entityMeta);

    public bool TryGetProfile(Guid profileId, out DbProfile? profile) =>
        _cache.TryGetValue(GetProfileKey(profileId), out profile);

    public void SetEntity(EntityMeta entityMeta) =>
        _cache.Set(GetEntityKey(entityMeta.EntityName), entityMeta, _options.Value.CacheTtl);

    public void SetProfile(DbProfile profile) =>
        _cache.Set(GetProfileKey(profile.ProfileId), profile, _options.Value.CacheTtl);

    public void RemoveEntity(string entityName) => _cache.Remove(GetEntityKey(entityName));

    public void RemoveProfile(Guid profileId) => _cache.Remove(GetProfileKey(profileId));

    public Task ClearAsync()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }

        return Task.CompletedTask;
    }

    internal static string GetEntityKey(string entityName) => $"entity::{entityName.Trim().ToLowerInvariant()}";

    internal static string GetProfileKey(Guid profileId) => $"profile::{profileId:D}";
}
