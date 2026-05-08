using Janatics.DataEngine.QueryEngine.Pipeline;
using Janatics.DataEngine.Security.Sanitization;

namespace Janatics.DataEngine.QueryEngine.Stages;

public sealed class SqlValidationStage(ISqlSanitizer sanitizer) : IFetchPipelineStage
{
    public int Order => 30;

    public Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        context.ResolvedSql = sanitizer.Sanitize(context.QueryDefinition!.SqlTemplate);
        return Task.CompletedTask;
    }
}
