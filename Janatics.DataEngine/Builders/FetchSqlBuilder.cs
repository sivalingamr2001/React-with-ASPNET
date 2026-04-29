using System.Text;
using Dapper;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Registry;

namespace Janatics.DataEngine.Builders;

/// <summary>
/// Builds provider-aware SELECT statements from fetch requests.
/// </summary>
public sealed class FetchSqlBuilder
{
    private readonly SqlDialect _dialect;
    private readonly int _maxPageSize;

    public FetchSqlBuilder(SqlDialect dialect, int maxPageSize = 1000)
    {
        _dialect = dialect;
        _maxPageSize = maxPageSize;
    }

    public SqlStatement Build(EntityMeta entityMeta, FetchRequest request)
    {
        var parameters = new DynamicParameters();
        var selectedColumns = BuildSelectedColumns(entityMeta, request.Columns);
        var fromClause = $" FROM {_dialect.QuoteIdentifier(entityMeta.QualifiedTableName)}";
        var whereClause = BuildWhereClause(entityMeta, request.Filters, request.Search, request.IncludeSoftDeleted, parameters);
        var orderClause = BuildOrderClause(entityMeta, request.Sort);
        var pagingClause = BuildPagingClause(request.Page, request.PageSize, parameters);
        var sql = $"SELECT {selectedColumns}{fromClause}{whereClause}{orderClause}{pagingClause}";
        var countSql = request.IncludeCount ? $"SELECT COUNT(1){fromClause}{whereClause}" : null;
        return new SqlStatement(sql, parameters, countSql);
    }

    public SqlStatement BuildNode(EntityMeta entityMeta, NodeRequest request, IReadOnlyCollection<object?> parentKeys)
    {
        var parameters = new DynamicParameters();
        parameters.Add("parentKeys", parentKeys.ToArray());

        var selectedColumns = BuildSelectedColumns(entityMeta, request.Columns);
        var fromClause = $" FROM {_dialect.QuoteIdentifier(entityMeta.QualifiedTableName)}";
        var whereParts = new List<string>
        {
            $"{_dialect.QuoteIdentifier(request.ParentColumn)} IN {SqlParameterList("parentKeys")}"
        };

        if (!string.IsNullOrWhiteSpace(entityMeta.SoftDeleteColumn))
        {
            whereParts.Add($"{_dialect.QuoteIdentifier(entityMeta.SoftDeleteColumn)} = 0");
        }

        AppendFilters(entityMeta, request.Filters, parameters, whereParts, request.Search);
        var orderClause = BuildOrderClause(entityMeta, request.Sort);
        var sql = new StringBuilder($"SELECT {selectedColumns}{fromClause} WHERE {string.Join(" AND ", whereParts)}{orderClause}");

        if (request.Limit is > 0)
        {
            parameters.Add("nodeLimit", request.Limit.Value);
            sql.Append($" LIMIT {_dialect.Parameter("nodeLimit")}");
        }

        return new SqlStatement(sql.ToString(), parameters);
    }

    private string BuildSelectedColumns(EntityMeta entityMeta, IReadOnlyList<string>? columns)
    {
        var selected = (columns is { Count: > 0 }
            ? columns
            : entityMeta.Columns.Values.OrderBy(static column => column.OrdinalPosition).Select(static column => column.ColumnName))
            .Select(_dialect.QuoteIdentifier);

        return string.Join(", ", selected);
    }

    private string BuildWhereClause(
        EntityMeta entityMeta,
        IReadOnlyList<FilterCondition>? filters,
        SearchOptions? search,
        bool includeSoftDeleted,
        DynamicParameters parameters)
    {
        var clauses = new List<string>();

        if (!includeSoftDeleted && !string.IsNullOrWhiteSpace(entityMeta.SoftDeleteColumn))
        {
            clauses.Add($"{_dialect.QuoteIdentifier(entityMeta.SoftDeleteColumn)} = 0");
        }

        AppendFilters(entityMeta, filters, parameters, clauses, search);

        return clauses.Count == 0 ? string.Empty : $" WHERE {string.Join(" AND ", clauses)}";
    }

    private void AppendFilters(
        EntityMeta entityMeta,
        IReadOnlyList<FilterCondition>? filters,
        DynamicParameters parameters,
        ICollection<string> clauses,
        SearchOptions? search)
    {
        var filterIndex = 0;
        if (filters is not null)
        {
            foreach (var filter in filters)
            {
                clauses.Add(BuildFilterClause(entityMeta, filter, parameters, filterIndex++));
            }
        }

        if (search is not null && !string.IsNullOrWhiteSpace(search.Term))
        {
            var searchColumns = search.Columns is { Count: > 0 }
                ? search.Columns
                : entityMeta.SearchableColumns.ToArray();

            if (searchColumns.Count > 0)
            {
                var searchClauses = new List<string>();
                foreach (var column in searchColumns)
                {
                    var parameterName = $"search_{filterIndex++}";
                    parameters.Add(parameterName, $"%{EscapeLike(search.Term)}%");
                    searchClauses.Add($"{_dialect.QuoteIdentifier(column)} LIKE {_dialect.Parameter(parameterName)} ESCAPE '\\'");
                }

                clauses.Add($"({string.Join(" OR ", searchClauses)})");
            }
        }
    }

    private string BuildOrderClause(EntityMeta entityMeta, IReadOnlyList<SortOptions>? sortOptions)
    {
        var sort = sortOptions is { Count: > 0 }
            ? sortOptions
            : new[] { new SortOptions(entityMeta.PrimaryKey) };

        return " ORDER BY " + string.Join(
            ", ",
            sort.Select(option => $"{_dialect.QuoteIdentifier(option.Column)} {(option.Descending ? "DESC" : "ASC")}"));
    }

    private string BuildPagingClause(int page, int pageSize, DynamicParameters parameters)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, _maxPageSize);
        parameters.Add("offset", (safePage - 1) * safePageSize);
        parameters.Add("limit", safePageSize);
        return $" {_dialect.ApplyPaging(string.Empty, "offset", "limit").Trim()}";
    }

    private string BuildFilterClause(EntityMeta entityMeta, FilterCondition filter, DynamicParameters parameters, int index)
    {
        var column = _dialect.QuoteIdentifier(filter.Column);
        var parameterName = $"p{index}";
        var operation = filter.Operator.Trim().ToLowerInvariant();

        return operation switch
        {
            "eq" => AddBinary(parameters, parameterName, column, "=", filter.Value),
            "neq" => AddBinary(parameters, parameterName, column, "<>", filter.Value),
            "gt" => AddBinary(parameters, parameterName, column, ">", filter.Value),
            "gte" => AddBinary(parameters, parameterName, column, ">=", filter.Value),
            "lt" => AddBinary(parameters, parameterName, column, "<", filter.Value),
            "lte" => AddBinary(parameters, parameterName, column, "<=", filter.Value),
            "contains" => AddLike(parameters, parameterName, column, filter.Value, "%{0}%"),
            "startswith" => AddLike(parameters, parameterName, column, filter.Value, "{0}%"),
            "endswith" => AddLike(parameters, parameterName, column, filter.Value, "%{0}"),
            "between" => AddBetween(parameters, parameterName, column, filter),
            "in" => AddIn(parameters, parameterName, column, filter),
            "isnull" => $"{column} IS NULL",
            "isnotnull" => $"{column} IS NOT NULL",
            _ => throw new SqlBuildException($"Filter operator '{filter.Operator}' is not supported for entity '{entityMeta.EntityName}'.")
        };
    }

    private string AddBinary(DynamicParameters parameters, string parameterName, string column, string op, object? value)
    {
        parameters.Add(parameterName, value);
        return $"{column} {op} {_dialect.Parameter(parameterName)}";
    }

    private string AddLike(DynamicParameters parameters, string parameterName, string column, object? value, string pattern)
    {
        var term = EscapeLike(Convert.ToString(value) ?? string.Empty);
        parameters.Add(parameterName, string.Format(pattern, term));
        return $"{column} LIKE {_dialect.Parameter(parameterName)} ESCAPE '\\'";
    }

    private string AddBetween(DynamicParameters parameters, string parameterName, string column, FilterCondition filter)
    {
        var start = $"{parameterName}_start";
        var end = $"{parameterName}_end";
        parameters.Add(start, filter.Value);
        parameters.Add(end, filter.SecondValue);
        return $"{column} BETWEEN {_dialect.Parameter(start)} AND {_dialect.Parameter(end)}";
    }

    private string AddIn(DynamicParameters parameters, string parameterName, string column, FilterCondition filter)
    {
        var values = filter.Values ?? (filter.Value is IEnumerable<object?> sequence && filter.Value is not string ? sequence.ToArray() : Array.Empty<object?>());
        if (values.Count == 0)
        {
            throw new SqlBuildException($"The 'in' filter for column '{filter.Column}' requires at least one value.");
        }

        parameters.Add(parameterName, values.ToArray());
        return $"{column} IN {SqlParameterList(parameterName)}";
    }

    private string SqlParameterList(string parameterName) => _dialect.Parameter(parameterName);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
