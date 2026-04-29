using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Registry;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine.Builders;

/// <summary>
/// Validates requests against registry metadata and engine rules.
/// </summary>
public sealed class WhitelistValidator
{
    private static readonly HashSet<string> FetchOperators = new(
        ["eq", "neq", "gt", "gte", "lt", "lte", "contains", "startswith", "endswith", "between", "in", "isnull", "isnotnull"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ForgeOperations = new(
        ["create", "update", "delete"],
        StringComparer.OrdinalIgnoreCase);

    private readonly DataEngineOptions _options;

    public WhitelistValidator(IOptions<DataEngineOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateFetch(EntityMeta entityMeta, FetchRequest request)
    {
        ValidateRoles(entityMeta, request.Roles);
        ValidateColumns(entityMeta, request.Columns);
        ValidateFilters(entityMeta, request.Filters);
        ValidateSort(entityMeta, request.Sort);
        ValidateSearch(entityMeta, request.Search);

        if (request.Page < 1)
        {
            throw new SqlBuildException("Page must be greater than zero.");
        }

        if (request.PageSize < 1 || request.PageSize > _options.MaxPageSize)
        {
            throw new SqlBuildException($"PageSize must be between 1 and {_options.MaxPageSize}.");
        }
    }

    public void ValidateNodeFetch(EntityMeta parentEntity, EntityMeta nodeEntity, NodeRequest node, IReadOnlyList<string>? roles)
    {
        _ = parentEntity;
        ValidateRoles(nodeEntity, roles);
        EnsureColumnExists(nodeEntity, node.ParentColumn);
        ValidateColumns(nodeEntity, node.Columns);
        ValidateFilters(nodeEntity, node.Filters);
        ValidateSort(nodeEntity, node.Sort);
        ValidateSearch(nodeEntity, node.Search);

        if (node.Limit is < 1)
        {
            throw new SqlBuildException($"Node '{node.Entity}' limit must be greater than zero.");
        }
    }

    public void ValidateForge(EntityMeta entityMeta, ForgeRequest request)
    {
        ValidateRoles(entityMeta, request.Roles);

        if (!ForgeOperations.Contains(request.Operation))
        {
            throw new SqlBuildException($"Forge operation '{request.Operation}' is not supported.");
        }

        if (entityMeta.IsReadOnly)
        {
            throw new ReadOnlyViolationException(entityMeta.EntityName);
        }

        var operation = request.Operation.Trim().ToLowerInvariant();
        switch (operation)
        {
            case "create":
                ValidateDataColumns(entityMeta, request.Data, includeReadOnly: false);
                ValidateRequiredColumns(entityMeta, request.Data);
                break;
            case "update":
                if (request.KeyValue is null)
                {
                    throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires a key value for update.");
                }

                ValidateDataColumns(entityMeta, request.Data, includeReadOnly: false);
                break;
            case "delete":
                if (request.KeyValue is null)
                {
                    throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires a key value for delete.");
                }

                if (string.IsNullOrWhiteSpace(entityMeta.SoftDeleteColumn))
                {
                    throw new SqlBuildException($"Entity '{entityMeta.EntityName}' does not support soft delete.");
                }

                break;
        }
    }

    public void ValidateNodeForge(EntityMeta entityMeta, NodeForge node, IReadOnlyList<string>? roles)
    {
        ValidateRoles(entityMeta, roles);
        EnsureColumnExists(entityMeta, node.ParentColumn);

        if (!ForgeOperations.Contains(node.Operation))
        {
            throw new SqlBuildException($"Child operation '{node.Operation}' is not supported.");
        }

        if (node.Rows.Count == 0)
        {
            throw new SqlBuildException($"Child node '{node.Entity}' requires at least one row.");
        }

        foreach (var row in node.Rows)
        {
            var keyColumn = node.KeyColumn ?? entityMeta.PrimaryKey;
            var validationRow = node.Operation.Equals("update", StringComparison.OrdinalIgnoreCase) ||
                                node.Operation.Equals("delete", StringComparison.OrdinalIgnoreCase)
                ? row.Where(pair => !pair.Key.Equals(keyColumn, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                : row;

            ValidateDataColumns(entityMeta, validationRow, includeReadOnly: false);

            if (node.Operation.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                ValidateRequiredColumns(entityMeta, row, node.ParentColumn);
            }

            if ((node.Operation.Equals("update", StringComparison.OrdinalIgnoreCase) ||
                 node.Operation.Equals("delete", StringComparison.OrdinalIgnoreCase)) &&
                !row.ContainsKey(keyColumn))
            {
                throw new SqlBuildException($"Child node '{node.Entity}' row is missing key column '{keyColumn}'.");
            }
        }
    }

    private static void ValidateColumns(EntityMeta entityMeta, IReadOnlyList<string>? columns)
    {
        if (columns is null)
        {
            return;
        }

        foreach (var column in columns)
        {
            EnsureColumnExists(entityMeta, column);
        }
    }

    private static void ValidateFilters(EntityMeta entityMeta, IReadOnlyList<FilterCondition>? filters)
    {
        if (filters is null)
        {
            return;
        }

        foreach (var filter in filters)
        {
            EnsureColumnExists(entityMeta, filter.Column);
            if (!FetchOperators.Contains(filter.Operator))
            {
                throw new SqlBuildException($"Filter operator '{filter.Operator}' is not supported.");
            }
        }
    }

    private static void ValidateSort(EntityMeta entityMeta, IReadOnlyList<SortOptions>? sortOptions)
    {
        if (sortOptions is null)
        {
            return;
        }

        foreach (var sort in sortOptions)
        {
            EnsureColumnExists(entityMeta, sort.Column);
        }
    }

    private static void ValidateSearch(EntityMeta entityMeta, SearchOptions? search)
    {
        if (search is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(search.Term))
        {
            throw new SqlBuildException("Search term cannot be empty.");
        }

        if (search.Columns is null)
        {
            return;
        }

        foreach (var column in search.Columns)
        {
            EnsureColumnExists(entityMeta, column);
        }
    }

    private static void ValidateDataColumns(EntityMeta entityMeta, IReadOnlyDictionary<string, object?>? data, bool includeReadOnly)
    {
        if (data is null)
        {
            throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires data.");
        }

        foreach (var column in data.Keys)
        {
            EnsureColumnExists(entityMeta, column);
            if (!includeReadOnly && entityMeta.Columns[column].IsReadOnly)
            {
                throw new ReadOnlyViolationException($"{entityMeta.EntityName}.{column}");
            }
        }
    }

    private static void ValidateRequiredColumns(EntityMeta entityMeta, IReadOnlyDictionary<string, object?>? data, string? ignoredColumn = null)
    {
        if (data is null)
        {
            throw new SqlBuildException($"Entity '{entityMeta.EntityName}' requires data.");
        }

        foreach (var requiredColumn in entityMeta.RequiredColumns)
        {
            if (ignoredColumn is not null && requiredColumn.Equals(ignoredColumn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!data.ContainsKey(requiredColumn))
            {
                throw new SqlBuildException($"Required column '{requiredColumn}' is missing for entity '{entityMeta.EntityName}'.");
            }
        }
    }

    private static void ValidateRoles(EntityMeta entityMeta, IReadOnlyList<string>? roles)
    {
        if (entityMeta.AllowedRoles.Count == 0)
        {
            return;
        }

        if (roles is null || roles.Count == 0)
        {
            throw new RoleViolationException(entityMeta.EntityName);
        }

        if (!roles.Any(role => entityMeta.AllowedRoles.Contains(role)))
        {
            throw new RoleViolationException(entityMeta.EntityName);
        }
    }

    private static void EnsureColumnExists(EntityMeta entityMeta, string columnName)
    {
        if (!entityMeta.Columns.ContainsKey(columnName))
        {
            throw new SqlBuildException($"Column '{columnName}' is not registered for entity '{entityMeta.EntityName}'.");
        }
    }
}
