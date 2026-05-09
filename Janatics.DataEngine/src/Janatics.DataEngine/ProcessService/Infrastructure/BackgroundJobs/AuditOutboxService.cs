using System.Data;
using System.Text.Json;
using Janatics.DataEngine.Abstractions;
using Janatics.DataEngine.Infrastructure.Models;
using Janatics.DataEngine.Models.Audit;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Janatics.DataEngine.Infrastructure.BackgroundJobs;

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
        if (connection is not NpgsqlConnection npgConnection || transaction is not NpgsqlTransaction npgTransaction)
            throw new InvalidOperationException("Audit outbox currently requires Npgsql transaction context.");

        const string sql = """
            INSERT INTO public.audit_outbox (id, payload, status, attempts, createdon)
            VALUES (@id, CAST(@payload AS jsonb), 'PENDING', 0, NOW())
            """;

        foreach (var job in jobs)
        {
            await using var cmd = new NpgsqlCommand(sql, npgConnection, npgTransaction);
            cmd.Parameters.Add(new NpgsqlParameter("@id", DbType.Guid) { Value = Guid.CreateVersion7() });
            cmd.Parameters.Add(new NpgsqlParameter("@payload", DbType.String) { Value = JsonSerializer.Serialize(job) });
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var processed = 0;
        await using var connection = new NpgsqlConnection(_databaseConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string claimSql = """
            SELECT id, payload
            FROM public.audit_outbox
            WHERE status = 'PENDING'
            ORDER BY createdon
            LIMIT 50
            """;

        var rows = new List<(Guid Id, string Payload)>();
        await using (var claimCmd = new NpgsqlCommand(claimSql, connection))
        await using (var reader = await claimCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1)));
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

    private static async Task MarkAsProcessedAsync(NpgsqlConnection connection, Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.audit_outbox
            SET status = 'PROCESSED', processedon = NOW()
            WHERE id = @id
            """;
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.Add(new NpgsqlParameter("@id", DbType.Guid) { Value = id });
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MarkAsFailedAsync(NpgsqlConnection connection, Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.audit_outbox
            SET attempts = attempts + 1,
                status = CASE WHEN attempts + 1 >= 10 THEN 'DEAD_LETTER' ELSE 'PENDING' END
            WHERE id = @id
            """;
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.Add(new NpgsqlParameter("@id", DbType.Guid) { Value = id });
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

        const string sql = """
            CREATE TABLE IF NOT EXISTS public.audit_outbox (
                id uuid PRIMARY KEY,
                payload jsonb NOT NULL,
                status varchar(20) NOT NULL,
                attempts integer NOT NULL DEFAULT 0,
                createdon timestamptz NOT NULL DEFAULT NOW(),
                processedon timestamptz NULL
            );
            CREATE INDEX IF NOT EXISTS ix_audit_outbox_status_createdon ON public.audit_outbox(status, createdon);
            """;

        await using var connection = new NpgsqlConnection(_databaseConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
