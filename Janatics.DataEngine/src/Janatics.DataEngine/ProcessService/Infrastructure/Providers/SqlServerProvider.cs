using Janatics.DataEngine.Core.Auditing;
using Janatics.DataEngine.Infrastructure.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Janatics.DataEngine.Infrastructure.Providers
{
    public class SqlServerProvider : IDataProvider
    {   
        private readonly string _connectionString;
        private readonly ILogger<SqlServerProvider> _logger;

        public SqlServerProvider(string connectionString, ILogger<SqlServerProvider> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public SqlServerProvider(DatabaseConfig config, ILogger<SqlServerProvider> logger)
            : this(config?.ConnectionString ?? throw new ArgumentNullException(nameof(config)), logger)
        {
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogDebug("SQL Server connection opened");
            return connection;
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            var transaction = connection.BeginTransaction();
            _logger.LogDebug("SQL Server transaction started");
            return await Task.FromResult(transaction);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        {
            using var command = new SqlCommand(sql, GetSqlConnection(transaction), GetSqlTransaction(transaction));
            AddParameters(command, parameters);
            
            _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);
            
            var result = await command.ExecuteNonQueryAsync();
            _logger.LogDebug("Rows affected: {RowsAffected}", result);
            
            return result;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        {
            using var command = new SqlCommand(sql, GetSqlConnection(transaction), GetSqlTransaction(transaction));
            AddParameters(command, parameters);
            
            _logger.LogDebug("Executing scalar SQL: {Sql}", sql);
            return await command.ExecuteScalarAsync();
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? await GetConnectionAsync();
            using var command = new SqlCommand(sql, (SqlConnection)connection, (SqlTransaction?)transaction);
            AddParameters(command, parameters);
            
            _logger.LogDebug("Executing query: {Sql}", sql);
            
            var dataTable = new DataTable();
            using var adapter = new SqlDataAdapter(command);
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
            
            // SQL Server OUTPUT syntax
            var sql = $"INSERT INTO {formattedTableName} ({string.Join(", ", processedColumns)}) OUTPUT INSERTED.{returnColumn} VALUES ({string.Join(", ", processedPlaceholders)})";
            
            _logger.LogDebug("Executing INSERT with OUTPUT: {Sql}", sql);
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
                    // Convert PostgreSQL NEXTVAL to SQL Server NEXT VALUE FOR
                    var sequenceName = stringValue.Replace("NEXTVAL('", "").Replace("')", "");
                    var sqlServerSequence = $"NEXT VALUE FOR {sequenceName}";
                    setClauses.Add($"{param.Key} = {sqlServerSequence}");
                    _logger.LogTrace("Using sequence function in UPDATE: {SequenceFunction} for column {Column}", sqlServerSequence, param.Key);
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
            // SQL Server supports bracket notation as-is
            // If no brackets, add them for safety
            if (!tableName.Contains("[") && tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                return $"[{parts[0]}].[{parts[1]}]";
            }
            return tableName;
        }

        private (List<string> columns, List<string> placeholders, Dictionary<string, object> parameters) ProcessParametersForSequences(Dictionary<string, object> originalParameters)
        {
            var columns = new List<string>();
            var placeholders = new List<string>();
            var parameters = new Dictionary<string, object>();
            
            foreach (var param in originalParameters)
            {
                columns.Add(param.Key);
                
                // Check if value is a sequence function call
                if (param.Value is string stringValue && stringValue.StartsWith("NEXTVAL("))
                {
                    // Convert PostgreSQL NEXTVAL to SQL Server NEXT VALUE FOR
                    var sequenceName = stringValue.Replace("NEXTVAL('", "").Replace("')", "");
                    var sqlServerSequence = $"NEXT VALUE FOR {sequenceName}";
                    placeholders.Add(sqlServerSequence);
                    _logger.LogTrace("Using sequence function: {SequenceFunction} for column {Column}", sqlServerSequence, param.Key);
                }
                else
                {
                    // Regular parameter
                    placeholders.Add(GetParameterPlaceholder(param.Key));
                    parameters[param.Key] = param.Value;
                }
            }
            
            return (columns, placeholders, parameters);
        }

        private void AddParameters(SqlCommand command, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                var parameterValue = param.Value ?? DBNull.Value;
                command.Parameters.AddWithValue($"@{param.Key}", parameterValue);
                _logger.LogTrace("Parameter: @{ParamName} = {ParamValue}", param.Key, parameterValue);
            }
        }

        public Task<int> ExecuteNonQueryAsync(string sql, NpgsqlCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction transaction)
        {
            var formattedTableName = FormatTableName(tableName);
            
            _logger.LogInformation("Starting SQL Server BulkCopy for table {TableName} with {RowCount} rows", tableName, dataTable.Rows.Count);
            
            using var bulkCopy = new SqlBulkCopy(GetSqlConnection(transaction), SqlBulkCopyOptions.Default, GetSqlTransaction(transaction))
            {
                DestinationTableName = formattedTableName,
                BatchSize = 1000,
                BulkCopyTimeout = 300
            };

            // Map columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable);
            
            _logger.LogInformation("SQL Server BulkCopy completed for table {TableName}, {RowCount} rows inserted", tableName, dataTable.Rows.Count);
            
            return dataTable.Rows.Count;
        }

        private static SqlConnection GetSqlConnection(IDbTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            return transaction.Connection as SqlConnection
                ?? throw new InvalidOperationException("The SQL Server provider requires an active SqlConnection on the supplied transaction.");
        }

        private static SqlTransaction GetSqlTransaction(IDbTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            return transaction as SqlTransaction
                ?? throw new InvalidOperationException("The SQL Server provider requires a SqlTransaction instance.");
        }
    }
}
