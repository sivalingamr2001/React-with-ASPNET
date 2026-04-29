using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Janatics.DataEngine.Services;

/// <summary>
/// Orchestrates request validation, SQL generation and execution for fetch operations.
/// </summary>
public sealed class FetchService : IFetchService
{
    private readonly IEntityRegistryService _registryService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly WhitelistValidator _validator;
    private readonly DataEngineOptions _options;
    private readonly ILogger<FetchService> _logger;

    public FetchService(
        IEntityRegistryService registryService,
        IDbConnectionFactory connectionFactory,
        WhitelistValidator validator,
        IOptions<DataEngineOptions> options,
        ILogger<FetchService> logger)
    {
        _registryService = registryService;
        _connectionFactory = connectionFactory;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FetchResult> FetchAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var entityMeta = await _registryService.GetEntityAsync(request.Entity, cancellationToken).ConfigureAwait(false);
        _validator.ValidateFetch(entityMeta, request);

        var profile = await _registryService.GetProfileAsync(entityMeta.ProfileId, cancellationToken).ConfigureAwait(false);
        var dialect = _connectionFactory.GetDialect(profile.Provider);
        var builder = new FetchSqlBuilder(dialect, _options.MaxPageSize);
        var statement = builder.Build(entityMeta, request);

        await using DbConnection connection = await _connectionFactory.CreateOpenConnectionAsync(profile, cancellationToken).ConfigureAwait(false);

        var rows = (await connection.QueryAsync(
            new CommandDefinition(statement.Sql, statement.Parameters, cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(MapRow)
            .ToList();

        if (request.Nodes is { Count: > 0 } && rows.Count > 0)
        {
            await PopulateNodesAsync(rows, request.Nodes, entityMeta, connection, dialect, request.Roles, cancellationToken).ConfigureAwait(false);
        }

        int? totalCount = request.IncludeCount && statement.CountSql is not null
            ? await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        statement.CountSql,
                        statement.Parameters,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false)
            : null;

        stopwatch.Stop();
        _logger.LogInformation(
            "DataEngine.FetchExecuted entity={Entity} provider={Provider} page={Page} pageSize={PageSize} rowCount={RowCount} totalCount={TotalCount} executionMs={ExecutionMs}",
            entityMeta.EntityName,
            profile.Provider,
            request.Page,
            request.PageSize,
            rows.Count,
            totalCount,
            stopwatch.ElapsedMilliseconds);

        return new FetchResult(true, rows, totalCount, request.Page, request.PageSize, entityMeta.EntityName, profile.Provider);
    }

    private async Task PopulateNodesAsync(
        List<Dictionary<string, object?>> rootRows,
        IReadOnlyList<NodeRequest> nodes,
        EntityMeta rootEntity,
        DbConnection connection,
        SqlDialect dialect,
        IReadOnlyList<string>? roles,
        CancellationToken cancellationToken)
    {
        var parentIds = rootRows
            .Select(row => row.TryGetValue(rootEntity.PrimaryKey, out var value) ? value : null)
            .Where(static value => value is not null)
            .Distinct()
            .ToArray();

        if (parentIds.Length == 0)
        {
            return;
        }

        foreach (var node in nodes)
        {
            var nodeEntity = await _registryService.GetEntityAsync(node.Entity, cancellationToken).ConfigureAwait(false);
            _validator.ValidateNodeFetch(rootEntity, nodeEntity, node, roles);

            var builder = new FetchSqlBuilder(dialect, _options.MaxPageSize);
            var statement = builder.BuildNode(nodeEntity, node, parentIds);
            var nodeRows = (await connection.QueryAsync(
                new CommandDefinition(statement.Sql, statement.Parameters, cancellationToken: cancellationToken)).ConfigureAwait(false))
                .Select(MapRow)
                .ToList();

            var grouped = nodeRows
                .Where(row => row.TryGetValue(node.ParentColumn, out var parentValue) && parentValue is not null)
                .GroupBy(row => row[node.ParentColumn]!)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<IReadOnlyDictionary<string, object?>>)group.Cast<IReadOnlyDictionary<string, object?>>().ToList());

            foreach (var rootRow in rootRows)
            {
                rootRow[node.Alias ?? node.Entity] = rootRow.TryGetValue(rootEntity.PrimaryKey, out var parentKey) && parentKey is not null && grouped.TryGetValue(parentKey, out var children)
                    ? children
                    : Array.Empty<IReadOnlyDictionary<string, object?>>();
            }
        }
    }

    private static Dictionary<string, object?> MapRow(dynamic row)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object> pair in (IDictionary<string, object>)row)
        {
            dictionary[pair.Key] = pair.Value is DBNull ? null : pair.Value;
        }

        return dictionary;
    }
}
