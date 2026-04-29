using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Janatics.DataEngine.Builders;
using Janatics.DataEngine.Infrastructure;
using Janatics.DataEngine.Models;
using Janatics.DataEngine.Registry;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.Services;

/// <summary>
/// Orchestrates request validation, SQL generation and transactional execution for forge operations.
/// </summary>
public sealed class ForgeService : IForgeService
{
    private readonly IEntityRegistryService _registryService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly WhitelistValidator _validator;
    private readonly ILogger<ForgeService> _logger;

    public ForgeService(
        IEntityRegistryService registryService,
        IDbConnectionFactory connectionFactory,
        WhitelistValidator validator,
        ILogger<ForgeService> logger)
    {
        _registryService = registryService;
        _connectionFactory = connectionFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ForgeResult> ForgeAsync(ForgeRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var entityMeta = await _registryService.GetEntityAsync(request.Entity, cancellationToken).ConfigureAwait(false);
        _validator.ValidateForge(entityMeta, request);

        var profile = await _registryService.GetProfileAsync(entityMeta.ProfileId, cancellationToken).ConfigureAwait(false);
        var dialect = _connectionFactory.GetDialect(profile.Provider);
        var builder = new ForgeSqlBuilder(dialect);

        await using DbConnection connection = await _connectionFactory.CreateOpenConnectionAsync(profile, cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var affectedRows = 0;
            object? rootId = request.KeyValue;

            switch (request.Operation.Trim().ToLowerInvariant())
            {
                case "create":
                    var createStatement = builder.BuildCreate(entityMeta, request.Data!);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(createStatement.Sql, createStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    rootId = rootId ?? request.Data!.GetValueOrDefault(entityMeta.PrimaryKey);
                    if (rootId is null && createStatement.IdentitySql is not null)
                    {
                        rootId = await connection.ExecuteScalarAsync<long>(
                            new CommandDefinition(createStatement.IdentitySql, transaction: transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    }

                    break;

                case "update":
                    var updateStatement = builder.BuildUpdate(entityMeta, request.Data!, request.KeyValue!);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(updateStatement.Sql, updateStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    rootId = request.KeyValue;
                    break;

                case "delete":
                    var deleteStatement = builder.BuildSoftDelete(entityMeta, request.KeyValue!);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(deleteStatement.Sql, deleteStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    rootId = request.KeyValue;
                    break;
            }

            if (request.Nodes is { Count: > 0 })
            {
                foreach (var node in request.Nodes)
                {
                    var nodeEntity = await _registryService.GetEntityAsync(node.Entity, cancellationToken).ConfigureAwait(false);
                    _validator.ValidateNodeForge(nodeEntity, node, request.Roles);
                    affectedRows += await ExecuteChildNodeAsync(connection, transaction, builder, nodeEntity, node, rootId, cancellationToken).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            _logger.LogInformation(
                "DataEngine.ForgeExecuted entity={Entity} provider={Provider} operation={Operation} entityId={EntityId} nodeCount={NodeCount} executionMs={ExecutionMs}",
                entityMeta.EntityName,
                profile.Provider,
                request.Operation,
                rootId,
                request.Nodes?.Count ?? 0,
                stopwatch.ElapsedMilliseconds);

            return new ForgeResult(true, rootId, request.Operation, affectedRows, entityMeta.EntityName, profile.Provider);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                exception,
                "DataEngine.RollbackExecuted entity={Entity} operation={Operation} errorMessage={ErrorMessage}",
                entityMeta.EntityName,
                request.Operation,
                exception.Message);
            throw;
        }
    }

    private static async Task<int> ExecuteChildNodeAsync(
        DbConnection connection,
        DbTransaction transaction,
        ForgeSqlBuilder builder,
        EntityMeta nodeEntity,
        NodeForge node,
        object? rootId,
        CancellationToken cancellationToken)
    {
        var affectedRows = 0;
        foreach (var row in node.Rows)
        {
            var mutableRow = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase)
            {
                [node.ParentColumn] = rootId
            };

            switch (node.Operation.Trim().ToLowerInvariant())
            {
                case "create":
                    var createStatement = builder.BuildCreate(nodeEntity, mutableRow);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(createStatement.Sql, createStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    break;

                case "update":
                    var keyColumn = node.KeyColumn ?? nodeEntity.PrimaryKey;
                    var keyValue = mutableRow[keyColumn];
                    mutableRow.Remove(keyColumn);
                    var updateStatement = builder.BuildUpdate(nodeEntity, mutableRow, keyValue!);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(updateStatement.Sql, updateStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    break;

                case "delete":
                    var deleteKey = mutableRow[node.KeyColumn ?? nodeEntity.PrimaryKey];
                    var deleteStatement = builder.BuildSoftDelete(nodeEntity, deleteKey!);
                    affectedRows += await connection.ExecuteAsync(
                        new CommandDefinition(deleteStatement.Sql, deleteStatement.Parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
                    break;
            }
        }

        return affectedRows;
    }
}
