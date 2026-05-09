using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Abstractions;
using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Infrastructure.Persistence;

public class QueryDefinitionRepository(
    FetchServiceOptions options,
    IResilientConnectionFactory connectionFactory) : IQueryDefinitionRepository
{
    private readonly FetchServiceOptions _options = options;
    private readonly IResilientConnectionFactory _connectionFactory = connectionFactory;

    public async Task<long> SaveAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();
        try
        {
            var queryNumber = request.QueryNumber.HasValue
                ? await UpdateAsync(connection, transaction, request, cancellationToken).ConfigureAwait(false)
                : await InsertAsync(connection, transaction, request, cancellationToken).ConfigureAwait(false);

            await ReplaceTablesAsync(connection, transaction, queryNumber, request.Tables, cancellationToken).ConfigureAwait(false);
            transaction.Commit();
            return queryNumber;
        }
        catch
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<QueryDefinitionModel?> GetAsync(long queryNumber, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        var sql = $"""
            SELECT querynumber, description, querytext, fetchjson::text, isfetchjson, parameterdefinition::text, isactive, createdby, createdon, modifiedby, modifiedon, versionno
            FROM {Qualified(_options.QueryDefinitionTableName)}
            WHERE querynumber = @querynumber
            """;

        using var command = CreateCommand(connection, sql);
        AddParameter(command, "@querynumber", queryNumber);
        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
        if (!await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            return null;

        var model = Map(reader);
        model.Tables = await GetTablesAsync(connection, queryNumber, cancellationToken).ConfigureAwait(false);
        return model;
    }

    public async Task<IReadOnlyList<QueryDefinitionModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        var sql = $"""
            SELECT querynumber, description, querytext, fetchjson::text, isfetchjson, parameterdefinition::text, isactive, createdby, createdon, modifiedby, modifiedon, versionno
            FROM {Qualified(_options.QueryDefinitionTableName)}
            WHERE isactive = TRUE
            ORDER BY querynumber DESC
            """;

        using var command = CreateCommand(connection, sql);
        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
        var items = new List<QueryDefinitionModel>();
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            items.Add(Map(reader));

        foreach (var item in items)
            item.Tables = await GetTablesAsync(connection, item.QueryNumber, cancellationToken).ConfigureAwait(false);

        return items;
    }

    public async Task<bool> DeleteAsync(long queryNumber, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        var sql = $"""
            UPDATE {Qualified(_options.QueryDefinitionTableName)}
            SET isactive = FALSE, modifiedby = @modifiedby, modifiedon = NOW(), versionno = versionno + 1
            WHERE querynumber = @querynumber
            """;
        using var command = CreateCommand(connection, sql);
        AddParameter(command, "@modifiedby", (object?)deletedBy ?? DBNull.Value);
        AddParameter(command, "@querynumber", queryNumber);
        return await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task RecordAuditAsync(FetchAuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        var sql = $"""
            INSERT INTO {Qualified(_options.QueryAuditLogTableName)}
                (querynumber, actionname, executedquery, parametersjson, resultcount, status, errormessage, createdby, createdon)
            VALUES
                (@querynumber, @actionname, @executedquery, CAST(@parametersjson AS jsonb), @resultcount, @status, @errormessage, @createdby, NOW())
            """;
        using var command = CreateCommand(connection, sql);
        AddParameter(command, "@querynumber", entry.QueryNumber);
        AddParameter(command, "@actionname", entry.ActionName);
        AddParameter(command, "@executedquery", (object?)entry.ExecutedQuery ?? DBNull.Value);
        AddParameter(command, "@parametersjson", string.IsNullOrWhiteSpace(entry.ParametersJson) ? "{}" : entry.ParametersJson);
        AddParameter(command, "@resultcount", (object?)entry.ResultCount ?? DBNull.Value);
        AddParameter(command, "@status", entry.Status);
        AddParameter(command, "@errormessage", (object?)entry.ErrorMessage ?? DBNull.Value);
        AddParameter(command, "@createdby", (object?)entry.CreatedBy ?? DBNull.Value);
        await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> InsertAsync(IDbConnection connection, IDbTransaction transaction, SaveQueryDefinitionRequest request, CancellationToken cancellationToken)
    {
        var sql = $"""
            INSERT INTO {Qualified(_options.QueryDefinitionTableName)}
                (description, querytext, fetchjson, isfetchjson, parameterdefinition, isactive, createdby, createdon, modifiedby, modifiedon, versionno)
            VALUES
                (@description, @querytext, CAST(@fetchjson AS jsonb), @isfetchjson, CAST(@parameterdefinition AS jsonb), @isactive, @createdby, NOW(), @modifiedby, NOW(), 1)
            RETURNING querynumber
            """;
        using var command = CreateCommand(connection, sql, transaction);
        AddSaveParameters(command, request);
        var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result);
    }

    private async Task<long> UpdateAsync(IDbConnection connection, IDbTransaction transaction, SaveQueryDefinitionRequest request, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE {Qualified(_options.QueryDefinitionTableName)}
            SET description = @description,
                querytext = @querytext,
                fetchjson = CAST(@fetchjson AS jsonb),
                isfetchjson = @isfetchjson,
                parameterdefinition = CAST(@parameterdefinition AS jsonb),
                isactive = @isactive,
                modifiedby = @modifiedby,
                modifiedon = NOW(),
                versionno = versionno + 1
            WHERE querynumber = @querynumber
            RETURNING querynumber
            """;
        using var command = CreateCommand(connection, sql, transaction);
        AddSaveParameters(command, request);
        AddParameter(command, "@querynumber", request.QueryNumber!.Value);
        var result = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException($"Query number {request.QueryNumber} was not found.");
        return Convert.ToInt64(result);
    }

    private void AddSaveParameters(IDbCommand command, SaveQueryDefinitionRequest request)
    {
        AddParameter(command, "@description", request.Description);
        AddParameter(command, "@querytext", (object?)request.QueryText ?? DBNull.Value);
        AddParameter(command, "@fetchjson", string.IsNullOrWhiteSpace(request.FetchJson) ? "{}" : request.FetchJson);
        AddParameter(command, "@isfetchjson", request.IsFetchJson);
        AddParameter(command, "@parameterdefinition", string.IsNullOrWhiteSpace(request.ParameterDefinitionJson) ? "{}" : request.ParameterDefinitionJson);
        AddParameter(command, "@isactive", request.IsActive);
        AddParameter(command, "@createdby", (object?)request.CreatedBy ?? DBNull.Value);
        AddParameter(command, "@modifiedby", request.ModifiedBy ?? request.CreatedBy ?? (object)DBNull.Value);
    }

    private async Task ReplaceTablesAsync(IDbConnection connection, IDbTransaction transaction, long queryNumber, IReadOnlyList<string> tables, CancellationToken cancellationToken)
    {
        var deleteSql = $"DELETE FROM {Qualified(_options.QueryTableMapTableName)} WHERE querynumber = @querynumber";
        using (var deleteCommand = CreateCommand(connection, deleteSql, transaction))
        {
            AddParameter(deleteCommand, "@querynumber", queryNumber);
            await ExecuteNonQueryAsync(deleteCommand, cancellationToken).ConfigureAwait(false);
        }

        foreach (var table in tables.Where(static x => !string.IsNullOrWhiteSpace(x)).Select(static x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var insertSql = $"""
                INSERT INTO {Qualified(_options.QueryTableMapTableName)} (querynumber, tablename, createdon)
                VALUES (@querynumber, @tablename, NOW())
                """;
            using var insertCommand = CreateCommand(connection, insertSql, transaction);
            AddParameter(insertCommand, "@querynumber", queryNumber);
            AddParameter(insertCommand, "@tablename", table);
            await ExecuteNonQueryAsync(insertCommand, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<string>> GetTablesAsync(IDbConnection connection, long queryNumber, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT tablename
            FROM {Qualified(_options.QueryTableMapTableName)}
            WHERE querynumber = @querynumber
            ORDER BY tablename
            """;
        using var command = CreateCommand(connection, sql);
        AddParameter(command, "@querynumber", queryNumber);
        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
        var tables = new List<string>();
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static QueryDefinitionModel Map(IDataReader reader)
    {
        return new QueryDefinitionModel
        {
            QueryNumber = reader.GetInt64(0),
            Description = reader.GetString(1),
            QueryText = reader.IsDBNull(2) ? null : reader.GetString(2),
            FetchJson = reader.IsDBNull(3) ? null : reader.GetString(3),
            IsFetchJson = !reader.IsDBNull(4) && reader.GetBoolean(4),
            ParameterDefinitionJson = reader.IsDBNull(5) ? null : reader.GetString(5),
            IsActive = !reader.IsDBNull(6) && reader.GetBoolean(6),
            CreatedBy = reader.IsDBNull(7) ? null : reader.GetString(7),
            CreatedOn = (DateTimeOffset)reader.GetValue(8),
            ModifiedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
            ModifiedOn = reader.IsDBNull(10) ? null : (DateTimeOffset)reader.GetValue(10),
            VersionNo = reader.GetInt32(11)
        };
    }

    private string Qualified(string tableName) => $"{_options.SchemaName}.{tableName}";

    private static IDbCommand CreateCommand(IDbConnection connection, string sql, IDbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return command.ExecuteScalar();
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return command.ExecuteNonQuery();
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        return command.ExecuteReader();
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbDataReader)
            return await dbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return reader.Read();
    }

    private static async Task RollbackAsync(IDbTransaction transaction)
    {
        try
        {
            if (transaction is DbTransaction dbTransaction)
                await dbTransaction.RollbackAsync().ConfigureAwait(false);
            else
                transaction.Rollback();
        }
        catch
        {
            // Ignore rollback failure.
        }
    }
}
