using Janatics.DataEngine.ProcessService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Janatics.DataEngine.Infrastructure.Persistence
{
    /// <summary>
    /// Repository for retrieving table metadata from public.applicationtable
    /// </summary>
    public class ApplicationTableMetadataRepository(
        IConfiguration configuration,
        ILogger<ApplicationTableMetadataRepository> logger)
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not found");
        private readonly ILogger<ApplicationTableMetadataRepository> _logger = logger;

        public async Task<TableAuditMetadata?> GetMetadataAsync(string entityName, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                const string sql = @"
                    SELECT tablename, enableaudit
                    FROM public.applicationtable
                    WHERE LOWER(tablename) = LOWER(@entityName)
                    LIMIT 1";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.Add(new NpgsqlParameter("@entityName", DbType.String) { Value = entityName });

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var tableName = reader.GetString(0);
                    var enableAudit = !reader.IsDBNull(1) && reader.GetBoolean(1);

                    return new TableAuditMetadata
                    {
                        TableName = tableName,
                        AuditEnabled = enableAudit
                    };
                }

                _logger.LogWarning("Table metadata not found for entity: {EntityName}", entityName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metadata for entity: {EntityName}", entityName);
                throw;
            }
        }
    }
}
