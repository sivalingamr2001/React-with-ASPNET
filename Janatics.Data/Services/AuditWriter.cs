using System.Data;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Writes audit records to PostgreSQL using ADO.NET
    /// </summary>
    public class AuditWriter
    {
        private readonly string _connectionString;
        private readonly ILogger<AuditWriter> _logger;
        private readonly AuditDiffBuilder _diffBuilder;

        public AuditWriter(
            DatabaseConfig databaseConfig,
            ILogger<AuditWriter> logger,
            AuditDiffBuilder diffBuilder)
        {
            _connectionString = databaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string not found");
            _logger = logger;
            _diffBuilder = diffBuilder;
        }

        public async Task WriteAuditAsync(AuditJob job, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Building changelog for {Table}/{Id}...",
                    job.TransactionTable,
                    job.TransactionId
                );

                var changeLog = await _diffBuilder.BuildChangeLogAsync(job, cancellationToken).ConfigureAwait(false);

                // Skip audit if no meaningful changes
                if (changeLog.Changes == null || changeLog.Changes.Count == 0)
                {
                    _logger.LogInformation(
                        "⊘ Skipping audit for {Table}/{Id} - No meaningful changes detected (all values null or unchanged)",
                        job.TransactionTable,
                        job.TransactionId
                    );
                    return;
                }

                var changeLogJson = _diffBuilder.SerializeChangeLog(changeLog);

                _logger.LogInformation(
                    "Connecting to database to write audit record...",
                    job.TransactionTable,
                    job.TransactionId
                );

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                _logger.LogInformation(
                    "Database connection opened. Executing INSERT command...",
                    job.TransactionTable,
                    job.TransactionId
                );

                const string sql = @"
                    INSERT INTO public.auditlog 
                        (transactiontable, transactionid, modifiedby, operation, changelog, createdon)
                    VALUES 
                        (@transactiontable, @transactionid, @modifiedby, @operation, @changelog::jsonb, @createdon)";

                await using var command = new NpgsqlCommand(sql, connection);
                
                command.Parameters.Add(new NpgsqlParameter("@transactiontable", DbType.String) 
                    { Value = job.TransactionTable });
                command.Parameters.Add(new NpgsqlParameter("@transactionid", DbType.Guid) 
                    { Value = job.TransactionId });
                command.Parameters.Add(new NpgsqlParameter("@modifiedby", DbType.String) 
                    { Value = job.ModifiedBy });
                command.Parameters.Add(new NpgsqlParameter("@operation", DbType.String) 
                    { Value = job.Operation.ToString() });
                command.Parameters.Add(new NpgsqlParameter("@changelog", DbType.String) 
                    { Value = changeLogJson });
                command.Parameters.Add(new NpgsqlParameter("@createdon", DbType.DateTimeOffset) 
                    { Value = job.TimestampUtc });

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "✓ Audit record written successfully for {Table}/{Id} - Operation: {Operation}",
                        job.TransactionTable,
                        job.TransactionId,
                        job.Operation
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "⚠ Audit INSERT returned 0 rows affected for {Table}/{Id}",
                        job.TransactionTable,
                        job.TransactionId
                    );
                }
            }
            catch (Exception ex)
            {
                // Swallow exception - audit failure should not impact main transaction
                _logger.LogError(
                    ex,
                    "✗ Failed to write audit record for {Table}/{Id}. Error: {ErrorMessage}",
                    job.TransactionTable,
                    job.TransactionId,
                    ex.Message
                );
            }
        }
    }
}
