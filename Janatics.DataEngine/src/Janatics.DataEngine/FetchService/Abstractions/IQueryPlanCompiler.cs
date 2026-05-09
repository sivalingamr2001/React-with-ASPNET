using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IQueryPlanCompiler
{
    CompiledQueryPlan Compile(string cacheKey, string sqlTemplate, int version);
}
