using System.Data;
using Npgsql;
using KATCRUDServices.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Providers
{
    public class PostgreSqlProvider : IDataProvider
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlProvider> _logger;

        public PostgreSqlProvider(string connectionString, ILogger<PostgreSqlProvider> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogDebug("PostgreSQL connection opened");
            return connection;
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            var transaction = connection.BeginTransaction();
            _logger.LogDebug("PostgreSQL transaction started");
            return await Task.FromResult(transaction);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        {
            if (transaction == null)
            {
                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)transaction.Connection);
                AddParameters(command, parameters);

                _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);

                var result = await command.ExecuteNonQueryAsync();
                _logger.LogDebug("Rows affected: {RowsAffected}", result);

                return result;
            }
            else
            {
                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)transaction.Connection, (NpgsqlTransaction)transaction);
                AddParameters(command, parameters);

                _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);

                var result = await command.ExecuteNonQueryAsync();
                _logger.LogDebug("Rows affected: {RowsAffected}", result);

                return result;
            }
        }
        public async Task<int> ExecuteNonQueryAsync(string sql, NpgsqlCommand command)
        {
            
           //   using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)command.Connection);
            // AddParameters(command, parameters);

            // _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);
            try
            {

                var result = await command.ExecuteNonQueryAsync();
                _logger.LogDebug("Rows affected: {RowsAffected}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing non-query SQL: {Sql}", sql);
                throw;
            }

                
            
        }

        public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        {
            using var command = new NpgsqlCommand(sql, (NpgsqlConnection)transaction.Connection, (NpgsqlTransaction)transaction);
            AddParameters(command, parameters);
            
            _logger.LogDebug("Executing scalar SQL: {Sql}", sql);
            return await command.ExecuteScalarAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? await GetConnectionAsync();
            using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction?)transaction);
            AddParameters(command, parameters);
            
            _logger.LogDebug("Executing query: {Sql}", sql);
            
            var dataTable = new DataTable();
            using var adapter = new NpgsqlDataAdapter(command);
            adapter.Fill(dataTable);
            
            if (transaction == null)
                connection.Close();
                
            _logger.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
            return dataTable;
        }

        public string GetParameterPlaceholder(string parameterName) => $"@{parameterName}";

        public async Task<object?> ExecuteInsertWithReturnAsync(string tableName, Dictionary<string, object> parameters, string returnColumn, IDbTransaction transaction)
        {
            var formattedTableName = FormatTableName(tableName);
            
            // Separate sequence values from regular parameters
            var (processedColumns, processedPlaceholders, processedParameters) = ProcessParametersForSequences(parameters);

            var quotedColumns = processedColumns
                .Select(col => $"\"{col}\"")
                .ToList();

            var sql =
                $"INSERT INTO {formattedTableName} ({string.Join(", ", quotedColumns)}) " +
                $"VALUES ({string.Join(", ", processedPlaceholders)}) RETURNING \"{returnColumn}\"";

            _logger.LogDebug("Executing INSERT with RETURNING: {Sql}", sql);
            return await ExecuteScalarAsync(sql, processedParameters, transaction);
        }

        public async Task<int> ExecuteUpdateAsync(string tableName, Dictionary<string, object> parameters, Dictionary<string, object> whereConditions, IDbTransaction transaction)
        {
            var formattedTableName = FormatTableName(tableName);
            
            // Build SET clause with sequence handling
            var setClauses = new List<string>();
            var allParameters = new Dictionary<string, object>();
            
            foreach (var param in parameters)
            {
                if (param.Value is string stringValue && stringValue.StartsWith("NEXTVAL("))
                {
                    // Use sequence function directly in SQL
                    setClauses.Add($"{param.Key} = {stringValue}");
                    _logger.LogTrace("Using sequence function in UPDATE: {SequenceFunction} for column {Column}", stringValue, param.Key);
                }
                else if (param.Value is string stringValue1 && stringValue1.StartsWith("SQL:"))
                {
                    // Raw SQL expression
                    var rawSql = stringValue1.Substring(4); // remove SQL:
                    setClauses.Add($"{param.Key} = {rawSql}");
                    _logger.LogTrace("Using raw SQL expression in UPDATE: {RawSql} for column {Column}", rawSql, param.Key);
                }
                else
                {
                    // Regular parameter
                    setClauses.Add($"{param.Key} = {GetParameterPlaceholder($"set_{param.Key}")}");
                    allParameters[$"set_{param.Key}"] = param.Value;
                }
            }
            
            // Build WHERE clause
            var whereClause = string.Join(" AND ", whereConditions.Keys.Select(key => $"{key} = {GetParameterPlaceholder($"where_{key}")}"));
            
            // Add WHERE parameters
            foreach (var param in whereConditions)
            {
                allParameters[$"where_{param.Key}"] = param.Value;
            }
            
            var sql = $"UPDATE {formattedTableName} SET {string.Join(", ", setClauses)} WHERE {whereClause}";
            
            _logger.LogDebug("Executing UPDATE: {Sql}", sql);
            return await ExecuteNonQueryAsync(sql, allParameters, transaction);
        }

        public async Task<int> ExecuteDeleteAsync(string tableName, Dictionary<string, object> whereConditions, IDbTransaction transaction)
        {
            var formattedTableName = FormatTableName(tableName);
            
            // Build WHERE clause
            var whereClause = string.Join(" AND ", whereConditions.Keys.Select(key => $"{key} = {GetParameterPlaceholder(key)}"));
            
            var sql = $"DELETE FROM {formattedTableName} WHERE {whereClause}";
            
            _logger.LogDebug("Executing DELETE: {Sql}", sql);
            return await ExecuteNonQueryAsync(sql, whereConditions, transaction);
        }

        public string FormatTableName(string tableName)
        {
            // Split by dot: schema.table
            var parts = tableName.Split('.');

            // Quote each part with double quotes
            var quotedParts = parts.Select(p => $"\"{p}\"");

            // Join back with dot
            return string.Join(".", quotedParts);
        }

        private (List<string> columns, List<string> placeholders, Dictionary<string, object> parameters) ProcessParametersForSequences(Dictionary<string, object> originalParameters)
        {
            var columns = new List<string>();
            var placeholders = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var param in originalParameters)
            {
                columns.Add(param.Key);

                if (param.Value is string stringValue)
                {
                    if (stringValue.StartsWith("NEXTVAL("))
                    {
                        // sequence
                        placeholders.Add(stringValue);
                        continue;
                    }

                    if (stringValue.StartsWith("SQL:"))
                    {
                        // raw SQL expression
                        placeholders.Add(stringValue.Substring(4)); // remove SQL:
                        continue;
                    }
                }

                // regular parameter
                placeholders.Add(GetParameterPlaceholder(param.Key));
                parameters[param.Key] = param.Value;
            }

            return (columns, placeholders, parameters);
        }

        private void AddParameters(NpgsqlCommand command, Dictionary<string, object> parameters)
        {
            if(parameters == null)
                return;
            foreach (var param in parameters)
            {
                var parameterValue = param.Value ?? DBNull.Value;

                // Auto-detect valid JSON strings and add with JSONB type for PostgreSQL
                if (parameterValue is string strVal && strVal.Length > 1 
                    && (strVal.StartsWith("{") || strVal.StartsWith("["))
                    && IsValidJson(strVal))
                {
                    var p = new NpgsqlParameter($"@{param.Key}", NpgsqlTypes.NpgsqlDbType.Jsonb)
                    {
                        Value = strVal
                    };
                    command.Parameters.Add(p);
                }
                else
                {
                    command.Parameters.AddWithValue($"@{param.Key}", parameterValue);
                }
                _logger.LogTrace("Parameter: @{ParamName} = {ParamValue}", param.Key, parameterValue);
            }
        }

        private static bool IsValidJson(string str)
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction transaction)
        {
            var formattedTableName = FormatTableName(tableName);
            var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            
            _logger.LogInformation("Starting PostgreSQL COPY for table {TableName} with {RowCount} rows", tableName, dataTable.Rows.Count);
            
            var copyCommand = $"COPY {formattedTableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";
            
            using var writer = ((NpgsqlConnection)transaction.Connection).BeginBinaryImport(copyCommand);
            
            foreach (DataRow row in dataTable.Rows)
            {
                writer.StartRow();
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    if (value == null || value == DBNull.Value)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        writer.Write(value);
                    }
                }
            }
            
            await writer.CompleteAsync();
            
            _logger.LogInformation("PostgreSQL COPY completed for table {TableName}, {RowCount} rows inserted", tableName, dataTable.Rows.Count);
            
            return dataTable.Rows.Count;
        }
    }
}