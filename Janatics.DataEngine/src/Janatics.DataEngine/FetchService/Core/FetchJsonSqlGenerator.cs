using System.Text;
using System.Text.Json;
using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.FetchService.Core;

internal class FetchJsonSqlGenerator(DatabaseProvider provider, ILogger logger)
{
    private readonly DatabaseProvider _provider = provider;
    private readonly ILogger _logger = logger;
    private int _paramCounter;

    public GeneratedFetchCommand GenerateSql(FetchJsonQuery query, IDictionary<string, object?> inputParameters)
    {
        _paramCounter = 0;
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var sql = new StringBuilder();

        sql.Append("SELECT ");
        BuildSelectClause(sql, query);
        sql.AppendLine();
        sql.Append($"FROM {QuoteIdentifier(query.Entity)} {QuoteIdentifier(query.Alias ?? query.Entity)}");

        foreach (var join in query.Joins)
        {
            sql.AppendLine();
            BuildJoinClause(sql, join);
        }

        if (query.Filter != null)
        {
            sql.AppendLine();
            sql.Append("WHERE ");
            BuildFilterClause(sql, query.Filter, parameters, inputParameters);
        }

        if (query.OrderBy.Any())
        {
            sql.AppendLine();
            sql.Append("ORDER BY ");
            sql.Append(string.Join(", ", query.OrderBy.Select(x => $"{QuoteIdentifier(x.Field)} {x.Direction.ToUpperInvariant()}")));
        }
        else
        {
            sql.AppendLine();
            sql.Append("ORDER BY (SELECT NULL)");
        }

        sql.AppendLine();
        BuildPaginationClause(sql, query);

        var finalSql = sql.ToString();
        _logger.LogInformation("Generated SQL from FetchJSON.");
        return new GeneratedFetchCommand { Sql = finalSql, Parameters = parameters };
    }

    private void BuildSelectClause(StringBuilder sql, FetchJsonQuery query)
    {
        var columns = new List<string>();
        var rootAlias = query.Alias ?? query.Entity;

        foreach (var column in query.Columns)
            columns.Add(column.Contains('.') ? QuoteIdentifier(column) : $"{QuoteIdentifier(rootAlias)}.{QuoteIdentifier(column)}");

        foreach (var join in query.Joins)
        {
            foreach (var column in join.Columns)
                columns.Add($"{QuoteIdentifier(join.Alias)}.{QuoteIdentifier(column)}");
        }

        sql.Append(string.Join(", ", columns));
    }

    private void BuildJoinClause(StringBuilder sql, FetchJsonJoin join)
    {
        var joinType = join.Type.ToUpperInvariant() switch
        {
            "LEFT" => "LEFT JOIN",
            "RIGHT" => "RIGHT JOIN",
            _ => "INNER JOIN"
        };

        sql.Append($"{joinType} {QuoteIdentifier(join.Entity)} {QuoteIdentifier(join.Alias)} ");
        sql.Append($"ON {QuoteIdentifier(join.From.EntityAlias)}.{QuoteIdentifier(join.From.Field)} = {QuoteIdentifier(join.Alias)}.{QuoteIdentifier(join.ToField)}");
    }

    private void BuildFilterClause(StringBuilder sql, FetchJsonFilter filter, Dictionary<string, object?> parameters, IDictionary<string, object?> inputParameters)
    {
        var conditions = new List<string>();

        if (filter.Conditions != null)
        {
            conditions.AddRange(filter.Conditions.Select(condition => BuildCondition(condition, parameters, inputParameters)));
        }

        if (filter.Groups != null)
        {
            foreach (var group in filter.Groups)
            {
                var builder = new StringBuilder();
                BuildFilterClause(builder, group, parameters, inputParameters);
                conditions.Add($"({builder})");
            }
        }

        var separator = string.Equals(filter.Operator, "or", StringComparison.OrdinalIgnoreCase) ? " OR " : " AND ";
        sql.Append(string.Join(separator, conditions));
    }

    private string BuildCondition(FetchJsonCondition condition, Dictionary<string, object?> parameters, IDictionary<string, object?> inputParameters)
    {
        var field = QuoteIdentifier(condition.Field);
        var parameterName = $"@p{_paramCounter++}";
        var value = ResolveValue(condition.Value, inputParameters);

        return condition.Operator.ToLowerInvariant() switch
        {
            "eq" => BuildEqualCondition(field, parameterName, value, parameters),
            "neq" => BuildNotEqualCondition(field, parameterName, value, parameters),
            "gt" => BuildComparisonCondition(field, ">", parameterName, value, parameters),
            "lt" => BuildComparisonCondition(field, "<", parameterName, value, parameters),
            "gte" => BuildComparisonCondition(field, ">=", parameterName, value, parameters),
            "lte" => BuildComparisonCondition(field, "<=", parameterName, value, parameters),
            "contains" => BuildLikeCondition(field, parameterName, value, parameters, "%{0}%"),
            "startswith" => BuildLikeCondition(field, parameterName, value, parameters, "{0}%"),
            "endswith" => BuildLikeCondition(field, parameterName, value, parameters, "%{0}"),
            "like" => BuildLikeCondition(field, parameterName, value, parameters, "{0}"),
            "in" => BuildInCondition(field, value, parameters),
            "notin" => BuildNotInCondition(field, value, parameters),
            "null" => $"{field} IS NULL",
            "notnull" => $"{field} IS NOT NULL",
            _ => throw new NotSupportedException($"Operator '{condition.Operator}' is not supported.")
        };
    }

    private static object? ResolveValue(object? value, IDictionary<string, object?> inputParameters)
    {
        if (value is string stringValue)
        {
            string? key = null;
            if (stringValue.StartsWith("{") && stringValue.EndsWith("}"))
                key = stringValue.Trim('{', '}');
            else if (stringValue.StartsWith("@"))
                key = stringValue[1..];

            if (key != null && inputParameters.TryGetValue(key, out var parameterValue))
                return parameterValue;
        }

        return value;
    }

    private static string BuildEqualCondition(string field, string parameterName, object? value, Dictionary<string, object?> parameters)
    {
        if (value == null)
            return $"{field} IS NULL";

        parameters[parameterName] = value;
        return $"{field} = {parameterName}";
    }

    private static string BuildNotEqualCondition(string field, string parameterName, object? value, Dictionary<string, object?> parameters)
    {
        if (value == null)
            return $"{field} IS NOT NULL";

        parameters[parameterName] = value;
        return $"{field} <> {parameterName}";
    }

    private static string BuildComparisonCondition(string field, string op, string parameterName, object? value, Dictionary<string, object?> parameters)
    {
        parameters[parameterName] = value;
        return $"{field} {op} {parameterName}";
    }

    private string BuildLikeCondition(string field, string parameterName, object? value, Dictionary<string, object?> parameters, string pattern)
    {
        parameters[parameterName] = string.Format(pattern, value?.ToString() ?? string.Empty);
        return _provider == DatabaseProvider.PostgreSQL ? $"{field} ILIKE {parameterName}" : $"{field} LIKE {parameterName}";
    }

    private string BuildInCondition(string field, object? value, Dictionary<string, object?> parameters)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            return BuildInConditionFromList(field, jsonElement.EnumerateArray().Select(x => (string?)x.ToString()).ToList(), parameters);

        if (value is IEnumerable<object> enumerable && value is not string)
            return BuildInConditionFromList(field, enumerable.Select(x => x?.ToString()).ToList(), parameters);

        throw new ArgumentException("IN operator requires an array value.");
    }

    private string BuildInConditionFromList(string field, List<string?> values, Dictionary<string, object?> parameters)
    {
        var parameterNames = new List<string>();
        foreach (var value in values)
        {
            var parameterName = $"@p{_paramCounter++}";
            parameters[parameterName] = value;
            parameterNames.Add(parameterName);
        }

        return $"{field} IN ({string.Join(", ", parameterNames)})";
    }

    private string BuildNotInCondition(string field, object? value, Dictionary<string, object?> parameters)
        => BuildInCondition(field, value, parameters).Replace(" IN ", " NOT IN ", StringComparison.Ordinal);

    private void BuildPaginationClause(StringBuilder sql, FetchJsonQuery query)
    {
        var offset = (query.Page - 1) * query.PageSize;
        if (_provider == DatabaseProvider.PostgreSQL || _provider == DatabaseProvider.MySQL)
            sql.Append($"LIMIT {query.PageSize} OFFSET {offset}");
        else
            sql.Append($"OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY");
    }

    private string QuoteIdentifier(string identifier)
    {
        if (identifier.Contains('.'))
            return string.Join(".", identifier.Split('.').Select(QuoteIdentifier));

        return _provider switch
        {
            DatabaseProvider.PostgreSQL => $"\"{identifier}\"",
            DatabaseProvider.SqlServer => $"[{identifier}]",
            DatabaseProvider.MySQL => $"`{identifier}`",
            _ => identifier
        };
    }
}
