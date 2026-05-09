using System.Data;
using System.Data.Common;
using Janatics.DataEngine.Abstractions;
using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Infrastructure.Persistence;

public sealed class MasterTablePreflight(
    DataEngineOptions options,
    FetchServiceOptions fetchOptions,
    IResilientConnectionFactory connectionFactory) : IMasterTablePreflight
{
    private static readonly string[] RequiredTables =
    [
        "applicationtable",
        "fieldmapper",
        "auditlog",
        "fetchquerydefinition",
        "fetchquerytablemap",
        "fetchqueryauditlog"
    ];

    private readonly DataEngineOptions _options = options;
    private readonly FetchServiceOptions _fetchOptions = fetchOptions;
    private readonly IResilientConnectionFactory _connectionFactory = connectionFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _validated;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.RequireMasterTables || _validated)
            return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_validated)
                return;

            using var connection = await _connectionFactory.CreateConnectionAsync(_fetchOptions.DatabaseConfig).ConfigureAwait(false);
            var missingTables = new List<string>();

            foreach (var table in RequiredTables)
            {
                var exists = await TableExistsAsync(connection, table, cancellationToken).ConfigureAwait(false);
                if (!exists)
                    missingTables.Add($"{_fetchOptions.SchemaName}.{table}");
            }

            if (missingTables.Count > 0)
            {
                var missing = string.Join(", ", missingTables);
                throw new InvalidOperationException(
                    $"Required master tables are missing: {missing}. Apply 'FetchService/Scripts/fetch-process-master-tables.postgresql.sql' before using this service.");
            }

            _validated = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> TableExistsAsync(IDbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM information_schema.tables
            WHERE table_schema = @schemaName
              AND table_name = @tableName
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var schemaParam = command.CreateParameter();
        schemaParam.ParameterName = "@schemaName";
        schemaParam.Value = _fetchOptions.SchemaName;
        command.Parameters.Add(schemaParam);

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var scalar = await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(scalar) > 0;
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return command.ExecuteScalar();
    }
}
