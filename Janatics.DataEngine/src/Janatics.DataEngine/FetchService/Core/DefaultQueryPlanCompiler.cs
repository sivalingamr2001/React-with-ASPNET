using System.Security.Cryptography;
using System.Text;
using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Core;

public sealed class DefaultQueryPlanCompiler : IQueryPlanCompiler
{
    public CompiledQueryPlan Compile(string cacheKey, string sqlTemplate, int version)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sqlTemplate));
        var hash = Convert.ToHexString(hashBytes);

        return new CompiledQueryPlan
        {
            CacheKey = cacheKey,
            SqlTemplate = sqlTemplate,
            Version = version,
            PlanHash = hash,
            CompiledAtUtc = DateTimeOffset.UtcNow
        };
    }
}
