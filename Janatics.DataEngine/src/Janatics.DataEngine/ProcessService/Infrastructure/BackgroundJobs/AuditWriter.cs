using Janatics.DataEngine.ProcessService.Core.Auditing;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Models.Audit;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Janatics.DataEngine.ProcessService.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Writes audit records to the configured database using ADO.NET
    /// </summary>
    public class AuditWriter(
        DatabaseConfig databaseConfig,
        ILogger<AuditWriter> logger,
        AuditDiffBuilder diffBuilder)
    {
        private readonly DatabaseConfig _databaseConfig = databaseConfig;
        private readonly string _connectionString = databaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string not found");
        private readonly ILogger<AuditWriter> _logger = logger;
        private readonly AuditDiffBuilder _diffBuilder = diffBuilder;

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
                    "Connecting to database to write audit record for {Table}/{Id}...",
                    job.TransactionTable,
                    job.TransactionId
                );

                await using var connection = CreateConnection();
                await connection.OpenAsync(cancellationToken);

                _logger.LogInformation(
                    "Database connection opened. Executing audit INSERT for {Table}/{Id}...",
                    job.TransactionTable,
                    job.TransactionId
                );

                var sql = _databaseConfig.Provider switch
                {
                    DatabaseProvider.PostgreSQL => @"
                        INSERT INTO public.auditlog 
                            (transactiontable, transactionid, modifiedby, operation, changelog, createdon)
                        VALUES 
                            (@transactiontable, @transactionid, @modifiedby, @operation, @changelog::jsonb, @createdon)",
                    DatabaseProvider.Sqlite => @"
                        INSERT INTO auditlog 
                            (transactiontable, transactionid, modifiedby, operation, changelog, createdon)
                        VALUES 
                            (@transactiontable, @transactionid, @modifiedby, @operation, @changelog, @createdon)",
                    _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported for audit.")
                };

                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                
                AddParameter(command, "@transactiontable", DbType.String, job.TransactionTable);
                AddParameter(command, "@transactionid", DbType.String, job.TransactionId.ToString());
                AddParameter(command, "@modifiedby", DbType.String, job.ModifiedBy);
                AddParameter(command, "@operation", DbType.String, job.Operation.ToString());
                AddParameter(command, "@changelog", DbType.String, changeLogJson);
                AddParameter(command, "@createdon", DbType.DateTimeOffset, job.TimestampUtc);

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

        private DbConnection CreateConnection()
        {
            return _databaseConfig.Provider switch
            {
                DatabaseProvider.PostgreSQL => new Npgsql.NpgsqlConnection(_connectionString),
                DatabaseProvider.Sqlite => new Microsoft.Data.Sqlite.SqliteConnection(_connectionString),
                DatabaseProvider.SqlServer => new Microsoft.Data.SqlClient.SqlConnection(_connectionString),
                DatabaseProvider.MySQL => new MySql.Data.MySqlClient.MySqlConnection(_connectionString),
                DatabaseProvider.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString),
                _ => throw new NotSupportedException($"Database provider '{_databaseConfig.Provider}' is not supported.")
            };
        }

        private static void AddParameter(DbCommand cmd, string parameterName, DbType dbType, object? value)
        {
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(parameter);
        }
    }
}

