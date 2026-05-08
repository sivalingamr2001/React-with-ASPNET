using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;

namespace Janatics.DataEngine.TransactionEngine.Commands;

public sealed class UpdateCommandBuilder : IUpdateCommandBuilder
{
    public async Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
        object identity,
        IDictionary<string, object?> content,
        IDbProvider provider,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct)
    {
        var fields = entity.Fields.ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);
        var writableMembers = content
            .Where(x => fields.TryGetValue(x.Key, out var field) && !field.IsReadOnly && !field.IsSystemField)
            .Where(x => !x.Key.Equals(entity.PrimaryKeyField, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var assignments = string.Join(", ", writableMembers.Select(x =>
            $"{provider.Dialect.QuoteIdentifier(fields[x.Key].ColumnName)} = {provider.Dialect.ParameterPrefix}{x.Key}"));

        var keyField = fields[entity.PrimaryKeyField];
        var sql = $"UPDATE {QuoteTable(entity, provider)} SET {assignments} WHERE {provider.Dialect.QuoteIdentifier(keyField.ColumnName)} = {provider.Dialect.ParameterPrefix}__id";

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        foreach (var member in writableMembers)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"{provider.Dialect.ParameterPrefix}{member.Key}";
            parameter.Value = member.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        var keyParameter = command.CreateParameter();
        keyParameter.ParameterName = $"{provider.Dialect.ParameterPrefix}__id";
        keyParameter.Value = identity;
        command.Parameters.Add(keyParameter);

        var affectedRows = await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
        return new NodeProcessingResult(entity.EntityKey, OperationType.Update, identity, affectedRows);
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
