using System.Data.Common;
using Janatics.DataEngine.Contracts.Dtos;
using Janatics.DataEngine.Contracts.Requests;
using Janatics.DataEngine.Contracts.Responses;
using Janatics.DataEngine.Domain.Exceptions;
using Janatics.DataEngine.Domain.Interfaces;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.TransactionEngine;

public sealed class TransactionEngineService(
    IMetadataRepository metadataRepository,
    INodeProcessor nodeProcessor,
    IDbProviderFactory providerFactory,
    IConnectionStringResolver connectionStringResolver,
    ILogger<TransactionEngineService> logger)
{
    public async Task<TransactionResponse> ExecuteAsync(TransactionRequest request, CancellationToken ct = default)
    {
        var rootEntity = await metadataRepository.GetEntityAsync(request.TenantCode, request.RootEntity, ct).ConfigureAwait(false)
            ?? throw new MetadataNotFoundException(request.RootEntity);

        var provider = providerFactory.Resolve(rootEntity.ProviderType);
        var connectionString = connectionStringResolver.Resolve(rootEntity.ConnectionName);
        using var connection = await provider.OpenConnectionAsync(connectionString, ct).ConfigureAwait(false);
        using var transaction = await BeginTransactionAsync(connection, ct).ConfigureAwait(false);

        var transactionId = Guid.NewGuid();
        var results = new List<NodeProcessingResult>();

        try
        {
            results.Add(await nodeProcessor.ProcessAsync(
                new NodeProcessingContext
                {
                    TenantCode = request.TenantCode,
                    EntityMetadata = rootEntity,
                    NodeId = request.RootId,
                    Content = request.Content,
                    Connection = connection,
                    Transaction = transaction
                },
                ct).ConfigureAwait(false));

            await ProcessChildrenAsync(
                request.TenantCode,
                request.Nodes,
                request.RootId,
                connection,
                transaction,
                results,
                ct).ConfigureAwait(false);

            await CommitAsync(transaction, ct).ConfigureAwait(false);

            return new TransactionResponse
            {
                TransactionId = transactionId,
                Succeeded = true,
                Results = results.Select(x => new NodeMutationResultDto(x.EntityKey, x.OperationType, x.Identity, x.AffectedRows)).ToList()
            };
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, ct).ConfigureAwait(false);
            logger.LogError(ex, "Failed to execute transaction for root entity {EntityKey}.", request.RootEntity);

            return new TransactionResponse
            {
                TransactionId = transactionId,
                Succeeded = false,
                ErrorMessage = ex.Message,
                Results = results.Select(x => new NodeMutationResultDto(x.EntityKey, x.OperationType, x.Identity, x.AffectedRows)).ToList()
            };
        }
    }

    private async Task ProcessChildrenAsync(
        string tenantCode,
        IReadOnlyList<TransactionNodeRequest> nodes,
        object? parentId,
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        ICollection<NodeProcessingResult> results,
        CancellationToken ct)
    {
        foreach (var node in nodes)
        {
            var entity = await metadataRepository.GetEntityAsync(tenantCode, node.NodeEntity, ct).ConfigureAwait(false)
                ?? throw new MetadataNotFoundException(node.NodeEntity);

            var result = await nodeProcessor.ProcessAsync(
                new NodeProcessingContext
                {
                    TenantCode = tenantCode,
                    EntityMetadata = entity,
                    NodeId = node.NodeId,
                    Content = node.Content,
                    IsDeleted = node.IsDeleted,
                    ParentKeyField = node.ParentLink,
                    ParentKeyValue = parentId,
                    Connection = connection,
                    Transaction = transaction
                },
                ct).ConfigureAwait(false);

            results.Add(result);

            if (node.Nodes.Count > 0)
            {
                await ProcessChildrenAsync(tenantCode, node.Nodes, result.Identity ?? node.NodeId, connection, transaction, results, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task<System.Data.IDbTransaction> BeginTransactionAsync(System.Data.IDbConnection connection, CancellationToken ct)
    {
        if (connection is DbConnection dbConnection)
        {
            return await dbConnection.BeginTransactionAsync(ct).ConfigureAwait(false);
        }

        return connection.BeginTransaction();
    }

    private static async Task CommitAsync(System.Data.IDbTransaction transaction, CancellationToken ct)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(ct).ConfigureAwait(false);
            return;
        }

        transaction.Commit();
    }

    private static async Task RollbackAsync(System.Data.IDbTransaction transaction, CancellationToken ct)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        transaction.Rollback();
    }
}
