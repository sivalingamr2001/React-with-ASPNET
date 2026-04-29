using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text.Json;
using System.Transactions;

namespace KATCRUDServices.Core.Repositories
{
    /// <summary>
    /// Static repository for managing DAG trigger configurations
    /// Stores and retrieves trigger settings from database
    /// No DI required - initialize with IDataProvider once during startup
    /// </summary>
    public static class DagTriggerRepository
    {
        private static readonly AsyncLocal<IDataProvider?> _asyncLocalDataProvider = new AsyncLocal<IDataProvider?>();
        private static IDataProvider? _globalDataProvider;

        private static IDataProvider? CurrentProvider => _asyncLocalDataProvider.Value ?? _globalDataProvider;

        /// <summary>
        /// Set the data provider for the repository
        /// This is called internally by TransactionService
        /// </summary>
        internal static void SetDataProvider(IDataProvider dataProvider)
        {
            if (_globalDataProvider == null)
            {
                _globalDataProvider = dataProvider;
                Console.WriteLine("[DagTriggerRepository] Global Data provider set");
            }
            // Always set the scoped wrapper for the current execution context
            _asyncLocalDataProvider.Value = dataProvider;
        }

        /// <summary>
        /// Get all active triggers for a table and operation type
        /// </summary>
        public static async Task<List<DagTriggerConfig>> GetTriggersForTableAsync(
            string tableName,
            string triggerType)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return new List<DagTriggerConfig>();
                }

                Console.WriteLine(
                    $"[DagTriggerRepository] Retrieving triggers for table {tableName}, type {triggerType}");

                // Check if table exists first




                var sql = @"
                    SELECT id, table_name, trigger, dag_id, enabled, cron_expression, trigger_type, insert_config, created_at, updated_at
                    FROM kat_dag_triggers
                    WHERE table_name ='" + tableName + "'";
                   sql =  sql + "AND trigger ='" + triggerType + "'    AND enabled = true";


                var dataTable = await CurrentProvider.ExecuteQueryAsync(sql,null);

                var triggers = new List<DagTriggerConfig>();
                foreach (DataRow row in dataTable.Rows)
                {
                    triggers.Add(new DagTriggerConfig
                    {
                        Id = Guid.Parse(row["id"].ToString() ?? Guid.NewGuid().ToString()),
                        TableName = row["table_name"].ToString() ?? string.Empty,
                        Trigger = row["trigger"].ToString() ?? string.Empty,
                        DagId = Guid.Parse(row["dag_id"].ToString() ?? Guid.Empty.ToString()),
                        Enabled = Convert.ToBoolean(row["enabled"]),
                        CronExpression = row["cron_expression"] != DBNull.Value ? row["cron_expression"].ToString() : null,
                        TriggerType = row["trigger_type"]?.ToString() ?? "DAGS",
                        InsertConfig = row["insert_config"] != DBNull.Value ? row["insert_config"].ToString() : null,
                        CreatedAt = Convert.ToDateTime(row["created_at"]),
                        UpdatedAt = Convert.ToDateTime(row["updated_at"])
                    });
                }

                Console.WriteLine($"[DagTriggerRepository] Found {triggers.Count} triggers for table {tableName}");
                return triggers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error retrieving triggers for table {tableName}: {ex.Message}");
                // Return empty list instead of throwing - table might not exist yet
                return new List<DagTriggerConfig>();
            }
        }

        /// <summary>
        /// Create a new DAG trigger configuration
        /// </summary>
        public static async Task<bool> CreateTriggerAsync(DagTriggerConfig trigger)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return false;
                }

                Console.WriteLine(
                    $"[DagTriggerRepository] Creating trigger for table {trigger.TableName}, type {trigger.Trigger}, DAG {trigger.DagId}");

                var sql = @"
                    INSERT INTO kat_dag_triggers 
                    (id, table_name, trigger, dag_id, enabled, cron_expression, trigger_type, insert_config, created_at, updated_at)
                    VALUES (@id, @tableName, @trigger, @dagId, @enabled, @cronExpression, @triggerType, @insertConfig, @createdAt, @updatedAt)";

                // Use direct connection for JSONB parameter handling
                using var connection = await CurrentProvider.GetConnectionAsync();
                using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection?)connection);

                cmd.Parameters.AddWithValue("@id", trigger.Id);
                cmd.Parameters.AddWithValue("@tableName", trigger.TableName);
                cmd.Parameters.AddWithValue("@trigger", trigger.Trigger);
                cmd.Parameters.AddWithValue("@dagId", trigger.DagId);
                cmd.Parameters.AddWithValue("@enabled", trigger.Enabled);
                cmd.Parameters.AddWithValue("@cronExpression", trigger.CronExpression ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@triggerType", trigger.TriggerType);
                
                // Handle JSONB parameter properly
                if (!string.IsNullOrEmpty(trigger.InsertConfig))
                {
                    cmd.Parameters.Add("@insertConfig", NpgsqlDbType.Jsonb).Value = trigger.InsertConfig;
                }
                else
                {
                    cmd.Parameters.AddWithValue("@insertConfig", DBNull.Value);
                }
                
                cmd.Parameters.AddWithValue("@createdAt", trigger.CreatedAt);
                cmd.Parameters.AddWithValue("@updatedAt", trigger.UpdatedAt);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error creating trigger for table {trigger.TableName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a DAG trigger configuration
        /// </summary>
        public static async Task<bool> DeleteTriggerAsync(Guid triggerId)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return false;
                }

                Console.WriteLine($"[DagTriggerRepository] Deleting trigger {triggerId}");

                var sql = "DELETE FROM kat_dag_triggers WHERE id = @id";
                var parameters = new Dictionary<string, object> { { "@id", triggerId } };

                var rowsAffected = await CurrentProvider.ExecuteNonQueryAsync(sql, parameters, null);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error deleting trigger {triggerId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update a DAG trigger configuration
        /// </summary>
        public static async Task<bool> UpdateTriggerAsync(DagTriggerConfig trigger)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return false;
                }

                Console.WriteLine($"[DagTriggerRepository] Updating trigger {trigger.Id}");

                var sql = @"
                    UPDATE kat_dag_triggers
                    SET table_name = @tableName,
                        trigger = @trigger,
                        dag_id = @dagId,
                        enabled = @enabled,
                        cron_expression = @cronExpression,
                        trigger_type = @triggerType,
                        insert_config = @insertConfig,
                        updated_at = @updatedAt
                    WHERE id = @id";

                // Use direct connection for JSONB parameter handling
                using var connection = await CurrentProvider.GetConnectionAsync();
                using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection?)connection);

                cmd.Parameters.AddWithValue("@id", trigger.Id);
                cmd.Parameters.AddWithValue("@tableName", trigger.TableName);
                cmd.Parameters.AddWithValue("@trigger", trigger.Trigger);
                cmd.Parameters.AddWithValue("@dagId", trigger.DagId);
                cmd.Parameters.AddWithValue("@enabled", trigger.Enabled);
                cmd.Parameters.AddWithValue("@cronExpression", trigger.CronExpression ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@triggerType", trigger.TriggerType);
                
                // Handle JSONB parameter properly
                if (!string.IsNullOrEmpty(trigger.InsertConfig))
                {
                    cmd.Parameters.Add("@insertConfig", NpgsqlDbType.Jsonb).Value = trigger.InsertConfig;
                }
                else
                {
                    cmd.Parameters.AddWithValue("@insertConfig", DBNull.Value);
                }
                
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error updating trigger {trigger.Id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Log a DAG trigger execution with context and response data
        /// </summary>
        public static async Task<bool> LogTriggerExecutionAsync(
            Guid triggerId,
            Guid dagId,
            string tableName,
            string triggerType,
            string? entityId,
            string? contextData,
            string? responseData,
            string status,
             
            string? errorMessage = null,
            int? httpStatusCode = null,
            int? executionTimeMs = null)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return false;
                }

                Console.WriteLine(
                    $"[DagTriggerRepository] Logging trigger execution for DAG {dagId}, status {status}");

                var sql = @"
                    INSERT INTO public.kat_dag_trigger_logs(
                        trigger_id,
                        dag_id,
                        table_name,
                        trigger_type,
                        entity_id,
                        context_data,
                        response_data,
                        status,
                        error_message,
                        http_status_code,
                        execution_time_ms,
                        created_at,
                        executed_at
                    ) VALUES (
                        @trigger_id,
                        @dag_id,
                        @table_name,
                        @trigger_type,
                        @entity_id,
                        @context_data,
                        @response_data,
                        @status,
                        @error_message,
                        @http_status_code,
                        @execution_time_ms,
                        @created_at,
                        @executed_at
                    ) RETURNING id;
                    ";

                // Use connection from pool - let the pool manage the connection lifecycle
                using var connection = await CurrentProvider.GetConnectionAsync();
                using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection?)connection);

                cmd.Parameters.AddWithValue("trigger_id", triggerId);
                cmd.Parameters.AddWithValue("dag_id", dagId);
                cmd.Parameters.AddWithValue("table_name", tableName);
                cmd.Parameters.AddWithValue("trigger_type", triggerType);
                cmd.Parameters.AddWithValue("entity_id", (object?)entityId ?? DBNull.Value);
                cmd.Parameters.Add("context_data", NpgsqlDbType.Jsonb).Value =
                    contextData == null ? DBNull.Value : JsonSerializer.Serialize(contextData);

                cmd.Parameters.Add("response_data", NpgsqlDbType.Jsonb).Value =
                    responseData == null ? DBNull.Value : JsonSerializer.Serialize(responseData);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("error_message", (object?)errorMessage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("http_status_code", (object?)httpStatusCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("execution_time_ms", (object?)executionTimeMs ?? DBNull.Value);

                var now = DateTime.UtcNow;
                cmd.Parameters.AddWithValue("created_at", now);
                cmd.Parameters.AddWithValue("executed_at", status == "Pending" ? (object)DBNull.Value : now);

                var rowsAffected = await CurrentProvider.ExecuteNonQueryAsync(sql, cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error logging trigger execution: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update trigger execution log with response data
        /// </summary>
        public static async Task<bool> UpdateTriggerLogAsync(
            Guid logId,
            string status,
            string? responseData = null,
            string? errorMessage = null,
            int? httpStatusCode = null,
            int? executionTimeMs = null)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return false;
                }

                Console.WriteLine($"[DagTriggerRepository] Updating trigger log {logId}, status {status}");

                var sql = @"
                    UPDATE kat_dag_trigger_logs
                    SET ""status"" = @status,
                        ""response_data"" = @responseData,
                        ""error_message"" = @errorMessage,
                        ""http_status_code"" = @httpStatusCode,
                        ""execution_time_ms"" = @executionTimeMs,
                        ""executed_at"" = @executedAt
                    WHERE ""id"" = @id";

                var parameters = new Dictionary<string, object>
                {
                    { "@id", logId },
                    { "@status", status },
                    { "@responseData", responseData ?? (object)DBNull.Value },
                    { "@errorMessage", errorMessage ?? (object)DBNull.Value },
                    { "@httpStatusCode", httpStatusCode ?? (object)DBNull.Value },
                    { "@executionTimeMs", executionTimeMs ?? (object)DBNull.Value },
                    { "@executedAt", DateTime.UtcNow }
                };

                var rowsAffected = await CurrentProvider.ExecuteNonQueryAsync(sql, parameters, null);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error updating trigger log: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get recent trigger logs for a DAG
        /// </summary>
        public static async Task<List<DagTriggerLog>> GetRecentLogsAsync(Guid dagId, int limit = 100)
        {
            try
            {
                if (CurrentProvider == null)
                {
                    Console.WriteLine("[DagTriggerRepository] Error: Data provider not initialized");
                    return new List<DagTriggerLog>();
                }

                var sql = @"
                    SELECT id, trigger_id, dag_id, table_name, trigger_type, entity_id, context_data, response_data, 
                           status, error_message, http_status_code, execution_time_ms, created_at, executed_at
                    FROM kat_dag_trigger_logs
                    WHERE dag_id = @dagId
                    ORDER BY created_at DESC
                    LIMIT @limit";

                var parameters = new Dictionary<string, object>
                {
                    { "@dagId", dagId },
                    { "@limit", limit }
                };

                var dataTable = await CurrentProvider.ExecuteQueryAsync(sql, parameters);

                var logs = new List<DagTriggerLog>();
                foreach (DataRow row in dataTable.Rows)
                {
                    logs.Add(new DagTriggerLog
                    {
                        Id = Guid.Parse(row["id"].ToString() ?? Guid.NewGuid().ToString()),
                        TriggerId = Guid.Parse(row["trigger_id"].ToString() ?? Guid.Empty.ToString()),
                        DagId = Guid.Parse(row["dag_id"].ToString() ?? Guid.Empty.ToString()),
                        TableName = row["table_name"].ToString() ?? string.Empty,
                        TriggerType = row["trigger_type"].ToString() ?? string.Empty,
                        EntityId = row["entity_id"] != DBNull.Value ? row["entity_id"].ToString() : null,
                        ContextData = row["context_data"] != DBNull.Value ? row["context_data"].ToString() : null,
                        ResponseData = row["response_data"] != DBNull.Value ? row["response_data"].ToString() : null,
                        Status = row["status"].ToString() ?? string.Empty,
                        ErrorMessage = row["error_message"] != DBNull.Value ? row["error_message"].ToString() : null,
                        HttpStatusCode = row["http_status_code"] != DBNull.Value ? Convert.ToInt32(row["http_status_code"]) : null,
                        ExecutionTimeMs = row["execution_time_ms"] != DBNull.Value ? Convert.ToInt32(row["execution_time_ms"]) : null,
                        CreatedAt = Convert.ToDateTime(row["created_at"]),
                        ExecutedAt = row["executed_at"] != DBNull.Value ? Convert.ToDateTime(row["executed_at"]) : null
                    });
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerRepository] Error retrieving trigger logs: {ex.Message}");
                return new List<DagTriggerLog>();
            }
        }
    }
}
