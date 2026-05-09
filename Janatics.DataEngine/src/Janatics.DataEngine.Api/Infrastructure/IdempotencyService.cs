using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Janatics.DataEngine.Api.Infrastructure;

public interface IIdempotencyService
{
    Task<string?> TryGetResponseAsync(string key, CancellationToken cancellationToken = default);
    Task SaveResponseAsync(string key, string payload, TimeSpan ttl, CancellationToken cancellationToken = default);
    string BuildScopedKey(HttpContext context, string externalKey, string requestBodyHash);
}

public sealed class IdempotencyService(
    IMemoryCache memoryCache,
    IServiceProvider serviceProvider) : IIdempotencyService
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<string?> TryGetResponseAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out string? memoryValue) && !string.IsNullOrWhiteSpace(memoryValue))
            return memoryValue;

        var distributed = _serviceProvider.GetService<IDistributedCache>();
        if (distributed == null)
            return null;

        return await distributed.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveResponseAsync(string key, string payload, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(key, payload, ttl);

        var distributed = _serviceProvider.GetService<IDistributedCache>();
        if (distributed != null)
        {
            await distributed.SetStringAsync(key, payload, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    public string BuildScopedKey(HttpContext context, string externalKey, string requestBodyHash)
    {
        var tenant = context.User.FindFirst("tenant")?.Value ?? "default";
        return $"idem:{tenant}:{externalKey}:{requestBodyHash}";
    }

    public static string ComputeRequestHash(string body)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(bytes);
    }
}
