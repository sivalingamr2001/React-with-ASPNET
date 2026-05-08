using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;

namespace Janatics.DataEngine.TransactionEngine.Commands;

public sealed class DeleteCommandBuilder : IDeleteCommandBuilder
{
    public async Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
        object identity,
        IDbProvider provider,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct)
    {
        var keyField = entity.Fields.First(x => x.FieldKey.Equals(entity.PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
        var sql = $"DELETE FROM {QuoteTable(entity, provider)} WHERE {provider.Dialect.QuoteIdentifier(keyField.ColumnName)} = {provider.Dialect.ParameterPrefix}__id";

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = $"{provider.Dialect.ParameterPrefix}__id";
        parameter.Value = identity;
        command.Parameters.Add(parameter);

        var affectedRows = await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
        return new NodeProcessingResult(entity.EntityKey, OperationType.Delete, identity, affectedRows);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken ct)
    {
        if (command is DbCommand dbCommand)
        {
            return await dbCommand.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        return command.ExecuteNonQuery();
    }

    private static string QuoteTable(MetadataEntity entity, IDbProvider provider)
        => string.IsNullOrWhiteSpace(entity.SchemaName)
            ? provider.Dialect.QuoteIdentifier(entity.TableName)
            : $"{provider.Dialect.QuoteIdentifier(entity.SchemaName)}.{provider.Dialect.QuoteIdentifier(entity.TableName)}";
}
