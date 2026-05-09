using System.Data;
using System.Data.Common;
using System.Text.Json;
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Models.Audit;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.ProcessService.Infrastructure.BackgroundJobs;

public sealed class AuditOutboxService(
    DatabaseConfig databaseConfig,
    AuditWriter auditWriter,
    ILogger<AuditOutboxService> logger) : IAuditOutboxService
{
    private readonly DatabaseConfig _databaseConfig = databaseConfig;
    private readonly AuditWriter _auditWriter = auditWriter;
    private readonly ILogger<AuditOutboxService> _logger = logger;
    private static bool _schemaEnsured;
    private static readonly Lock Sync = new();

    public async Task StageAsync(IEnumerable<AuditJob> jobs, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        
        var dbConnection = connection as DbConnection ?? throw new InvalidOperationException("Connection must be a DbConnection.");
        var dbTransaction = transaction as DbTransaction ?? throw new InvalidOperationException("Transaction must be a DbTransaction.");

        var sql = _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => """
                INSERT INTO public.audit_outbox (id, payload, status, attempts, createdon)
                VALUES (@id, @payload, 'PENDING', 0, NOW())
                """,
            DatabaseProvider.Sqlite => """
                INSERT INTO audit_outbox (id, payload, status, attempts, createdon)
                VALUES (@id, @payload, 'PENDING', 0, DATETIME('now'))
                """,
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit outbox.")
        };

        foreach (var job in jobs)
        {
            await using var cmd = dbConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = dbTransaction;
            cmd.Parameters.Add(CreateParameter(cmd, "@id", DbType.String, Guid.CreateVersion7().ToString()));
            cmd.Parameters.Add(CreateParameter(cmd, "@payload", DbType.String, JsonSerializer.Serialize(job)));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var processed = 0;
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var claimSql = _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => """
                SELECT id, payload
                FROM public.audit_outbox
                WHERE status = 'PENDING'
                ORDER BY createdon
                LIMIT 50
                """,
            DatabaseProvider.Sqlite => """
                SELECT id, payload
                FROM audit_outbox
                WHERE status = 'PENDING'
                ORDER BY createdon
                LIMIT 50
                """,
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit outbox.")
        };

        var rows = new List<(string Id, string Payload)>();
        await using (var claimCmd = connection.CreateCommand())
        {
            claimCmd.CommandText = claimSql;
            await using var reader = await claimCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var row in rows)
        {
            try
            {
                var job = JsonSerializer.Deserialize<AuditJob>(row.Payload);
                if (job != null)
                    await _auditWriter.WriteAuditAsync(job, cancellationToken).ConfigureAwait(false);

                await MarkAsProcessedAsync(connection, row.Id, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing audit outbox message {OutboxId}", row.Id);
                await MarkAsFailedAsync(connection, row.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        return processed;
    }

    private async Task MarkAsProcessedAsync(DbConnection connection, string id, CancellationToken cancellationToken)
    {
        var sql = _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => """
                UPDATE public.audit_outbox
                SET status = 'PROCESSED', processedon = NOW()
                WHERE id = @id
                """,
            DatabaseProvider.Sqlite => """
                UPDATE audit_outbox
                SET status = 'PROCESSED', processedon = DATETIME('now')
                WHERE id = @id
                """,
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit outbox.")
        };
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(CreateParameter(cmd, "@id", DbType.String, id));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(DbConnection connection, string id, CancellationToken cancellationToken)
    {
        var sql = _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => """
                UPDATE public.audit_outbox
                SET attempts = attempts + 1,
                    status = CASE WHEN attempts + 1 >= 10 THEN 'DEAD_LETTER' ELSE 'PENDING' END
                WHERE id = @id
                """,
            DatabaseProvider.Sqlite => """
                UPDATE audit_outbox
                SET attempts = attempts + 1,
                    status = CASE WHEN attempts + 1 >= 10 THEN 'DEAD_LETTER' ELSE 'PENDING' END
                WHERE id = @id
                """,
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit outbox.")
        };
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(CreateParameter(cmd, "@id", DbType.String, id));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaEnsured)
            return;

        lock (Sync)
        {
            if (_schemaEnsured)
                return;
            _schemaEnsured = true;
        }

        var sql = _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => """
                CREATE TABLE IF NOT EXISTS public.audit_outbox (
                    id uuid PRIMARY KEY,
                    payload jsonb NOT NULL,
                    status varchar(20) NOT NULL,
                    attempts integer NOT NULL DEFAULT 0,
                    createdon timestamptz NOT NULL DEFAULT NOW(),
                    processedon timestamptz NULL
                );
                CREATE INDEX IF NOT EXISTS ix_audit_outbox_status_createdon ON public.audit_outbox(status, createdon);
                """,
            DatabaseProvider.Sqlite => """
                CREATE TABLE IF NOT EXISTS audit_outbox (
                    id TEXT PRIMARY KEY,
                    payload TEXT NOT NULL,
                    status TEXT NOT NULL,
                    attempts INTEGER NOT NULL DEFAULT 0,
                    createdon DATETIME NOT NULL DEFAULT (DATETIME('now')),
                    processedon DATETIME NULL
                );
                CREATE INDEX IF NOT EXISTS ix_audit_outbox_status_createdon ON audit_outbox(status, createdon);
                """,
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit outbox.")
        };

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private DbConnection CreateConnection()
    {
        return _databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => new Npgsql.NpgsqlConnection(_databaseConfig.ConnectionString),
            DatabaseProvider.Sqlite => new Microsoft.Data.Sqlite.SqliteConnection(_databaseConfig.ConnectionString),
            DatabaseProvider.SqlServer => new Microsoft.Data.SqlClient.SqlConnection(_databaseConfig.ConnectionString),
            DatabaseProvider.MySQL => new MySql.Data.MySqlClient.MySqlConnection(_databaseConfig.ConnectionString),
            DatabaseProvider.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(_databaseConfig.ConnectionString),
            _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported.")
        };
    }

    private static DbParameter CreateParameter(DbCommand cmd, string parameterName, DbType dbType, object? value)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }
}
