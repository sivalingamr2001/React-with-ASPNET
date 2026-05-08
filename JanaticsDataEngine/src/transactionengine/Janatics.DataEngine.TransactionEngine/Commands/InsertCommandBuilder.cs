using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.Providers.Abstractions;
using Janatics.DataEngine.TransactionEngine.Orchestration;

namespace Janatics.DataEngine.TransactionEngine.Commands;

public sealed class InsertCommandBuilder : IInsertCommandBuilder
{
    public async Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
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

        var columns = string.Join(", ", writableMembers.Select(x => provider.Dialect.QuoteIdentifier(fields[x.Key].ColumnName)));
        var parameters = string.Join(", ", writableMembers.Select(x => $"{provider.Dialect.ParameterPrefix}{x.Key}"));
        var tableName = QuoteTable(entity, provider);
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

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

        var affectedRows = await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
        content.TryGetValue(entity.PrimaryKeyField, out var identity);
        return new NodeProcessingResult(entity.EntityKey, OperationType.Insert, identity, affectedRows);
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
