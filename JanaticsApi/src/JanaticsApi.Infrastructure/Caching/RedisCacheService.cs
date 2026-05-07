// src/EnterpriseApi.Infrastructure/Caching/RedisCacheService.cs
using JanaticsApi.Application.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace JanaticsApi.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _distributedCache.GetStringAsync(key, cancellationToken);

            if (data is null)
                return default;

            return JsonSerializer.Deserialize<T>(data, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {CacheKey}. Returning null.", key);
            return default; // Fail open — cache miss is acceptable
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();

            if (absoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(absoluteExpiration.Value);
            else
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(15)); // Sensible default

            var data = JsonSerializer.Serialize(value, SerializerOptions);
            await _distributedCache.SetStringAsync(key, data, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {CacheKey}. Continuing without cache.", key);
            // Fail open — continue without caching
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached is not null)
            return cached;

        var result = await factory(cancellationToken);

        if (result is not null)
            await SetAsync(key, result, absoluteExpiration, cancellationToken);

        return result;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: $"*{pattern}*").ToList();

            if (keys.Count > 0)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(keys.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache pattern REMOVE failed for pattern {Pattern}.", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS check failed for key {CacheKey}.", key);
            return false;
        }
    }
}