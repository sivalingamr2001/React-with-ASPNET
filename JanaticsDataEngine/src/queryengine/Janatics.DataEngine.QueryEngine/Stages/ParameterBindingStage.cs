using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.QueryEngine.Pipeline;

namespace Janatics.DataEngine.QueryEngine.Stages;

public sealed class ParameterBindingStage : IFetchPipelineStage
{
    public int Order => 20;

    public Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        var definition = context.QueryDefinition!;
        var boundParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in definition.Parameters.OrderBy(x => x.SortOrder))
        {
            if (context.Parameters.TryGetValue(parameter.ParameterKey, out var value))
            {
                boundParameters[parameter.ParameterKey] = CoerceValue(value, parameter.DataType);
            }
            else if (parameter.IsRequired)
            {
                context.Terminate($"Required parameter missing: {parameter.ParameterKey}");
                return Task.CompletedTask;
            }
            else if (parameter.DefaultValue is not null)
            {
                boundParameters[parameter.ParameterKey] = CoerceValue(parameter.DefaultValue, parameter.DataType);
            }
        }

        if (definition.IsPaginationEnabled && context.Pagination is not null)
        {
            boundParameters["__offset"] = context.Pagination.Offset;
            boundParameters["__limit"] = context.Pagination.PageSize;
        }

        context.BoundParameters = boundParameters;
        return Task.CompletedTask;
    }

    private static object? CoerceValue(object? value, FieldDataType targetType) => targetType switch
    {
        FieldDataType.String => value?.ToString(),
        FieldDataType.Integer => value is null ? null : Convert.ToInt64(value),
        FieldDataType.Decimal => value is null ? null : Convert.ToDecimal(value),
        FieldDataType.Boolean => value is null ? null : Convert.ToBoolean(value),
        FieldDataType.DateTime => value is null ? null : Convert.ToDateTime(value),
        FieldDataType.Guid => value is null ? null : Guid.Parse(value.ToString()!),
        _ => value
    };
}
