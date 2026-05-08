using DataEngine.Infrastruture.ConnectionFactory;
using DataEngine.Infrastruture.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace DataEngine.Providers;

/// <summary>
/// Enhanced PostgreSQL provider that implements IEnhancedDataProvider with optimized connection management,
/// connection sharing, and advanced monitoring capabilities
/// </summary>
public class PostgreSqlProvider(
    IResilientConnectionFactory connectionFactory,
    DatabaseConfig databaseConfig,
    ILogger<PostgreSqlProvider> logger) : IResilientDataProvider
{
    private readonly IResilientConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseConfig _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
    private readonly ILogger<PostgreSqlProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region IDataProvider Implementation (Legacy Methods)

    public async Task<IDbConnection> GetConnectionAsync()
    {
        return await _connectionFactory.CreateConnectionAsync(_databaseConfig);
    }

    public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
    {
        var transaction = connection.BeginTransaction();
        _logger.LogDebug("PostgreSQL transaction started");
        return await Task.FromResult(transaction);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
    {
        using var command = new NpgsqlCommand(sql, (NpgsqlConnection)transaction.Connection, (NpgsqlTransaction)transaction);
        AddParameters(command, parameters);

        _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);

        var result = await command.ExecuteNonQueryAsync();
        _logger.LogDebug("Rows affected: {RowsAffected}", result);

        return result;
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, NpgsqlCommand command)
    {
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

    public string GetParameterPlaceholder(string parameterName) => $"@{parameterName}";

    public string FormatTableName(string tableName)
    {
        // Split by dot: schema.table
        var parts = tableName.Split('.');

        // Quote each part with double quotes
        var quotedParts = parts.Select(p => $"\"{p}\"");

        // Join back with dot
        return string.Join(".", quotedParts);
    }

    #endregion

    #region IEnhancedDataProvider Implementation (New Enhanced Methods)

    /// <summary>
    /// Executes an operation within a connection scope, ensuring single connection usage
    /// </summary>
    public async Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation)
    {
        await using var scope = await CreateOperationScopeAsync();
        return await operation(scope.Connection);
    }

    /// <summary>
    /// Executes an operation within a transaction scope, ensuring single connection and transaction usage
    /// </summary>
    public async Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
    {
        await using var scope = await CreateOperationScopeAsync();
        using var transaction = await BeginTransactionAsync(scope.Connection);
        T? result;

        bool isCommit = false;

		try
        {
            result = await operation(scope.Connection, transaction);
            isCommit = true;
            //transaction.Commit();
            return result;
        }
        catch
        {
            isCommit = false;
            //transaction.Rollback();
            throw;
        }
		finally
		{
            await TerminateOtherIdleConnectionsAsync(scope.Connection);

            if (isCommit)
                transaction.Commit();
            else
                transaction.Rollback();

            if (scope.Connection.State == ConnectionState.Open)
            {
                scope.Connection.Close();
                await scope.DisposeAsync();
            }

            // Run cleanup in background (fire-and-forget)
            //         var connectionToCleanup = scope.Connection;
            //_ = Task.Run(async () =>
            //{
            //	try
            //	{
            //		await TerminateOtherIdleConnectionsAsync(scope.Connection);
            //		connectionToCleanup?.Dispose();
            //	}
            //	catch (Exception ex)
            //	{
            //		_logger.LogError(ex, "Error during background connection cleanup for transaction {TransactionId}");
            //	}
            //});
            // Clean up OTHER idle connections using the same connection

        }
	}


    /// <summary>
    /// Terminates other idle connections, using a separate connection if the current one is in a failed state
    /// </summary>
    private async Task TerminateOtherIdleConnectionsAsync(IDbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection ||
            npgsqlConnection.State != ConnectionState.Open)
        {
            return;
        }

        try
        {
            const string sql = @"
			SELECT pid
            FROM pg_stat_activity
            WHERE state = 'idle'
              AND pid <> pg_backend_pid()
              AND datname = current_database()
              AND usename = current_user
              AND state_change < NOW() - INTERVAL '60 seconds'
            ORDER BY state_change ASC;
		    ";

            using var command = new NpgsqlCommand(sql, npgsqlConnection)
            {
                CommandTimeout = 5
            };

            var pids = new List<int>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    pids.Add(reader.GetInt32(0));
                }
            }

            foreach (var pid in pids)
            {
                using var terminateCmd = new NpgsqlCommand(
                    "SELECT pg_terminate_backend(@pid);",
                    npgsqlConnection);

                terminateCmd.Parameters.AddWithValue("pid", pid);
                await terminateCmd.ExecuteNonQueryAsync();
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "25P02")
        {
            _logger.LogDebug(
                "Cannot perform idle connection cleanup because the transaction is aborted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while terminating idle PostgreSQL connections");
        }
    }


    /// <summary>
    /// Connection-aware ExecuteNonQueryAsync that uses provided connection
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction?)transaction);
        AddParameters(command, parameters);

        _logger.LogDebug("Executing SQL with shared connection: {Sql} with {ParameterCount} parameters", sql, parameters.Count);

        var result = await command.ExecuteNonQueryAsync();
        _logger.LogDebug("Rows affected: {RowsAffected}", result);

        return result;
    }

    /// <summary>
    /// Connection-aware ExecuteScalarAsync that uses provided connection
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction?)transaction);
        AddParameters(command, parameters);
        
        _logger.LogDebug("Executing scalar SQL with shared connection: {Sql}", sql);
        return await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Connection-aware ExecuteQueryAsync that uses provided connection
    /// </summary>
    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction?)transaction);
        AddParameters(command, parameters);
        
        _logger.LogDebug("Executing query with shared connection: {Sql}", sql);
        
        var dataTable = new DataTable();
        using var adapter = new NpgsqlDataAdapter(command);
        adapter.Fill(dataTable);
            
        _logger.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    /// <summary>
    /// Connection-aware BulkInsertAsync that uses provided connection
    /// </summary>
    public async Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null)
    {
        var formattedTableName = FormatTableName(tableName);
        var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        
        _logger.LogInformation("Starting PostgreSQL COPY with shared connection for table {TableName} with {RowCount} rows", tableName, dataTable.Rows.Count);
        
        var copyCommand = $"COPY {formattedTableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";
        
        using var writer = ((NpgsqlConnection)connection).BeginBinaryImport(copyCommand);
        
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
        
        _logger.LogInformation("PostgreSQL COPY completed with shared connection for table {TableName}, {RowCount} rows inserted", tableName, dataTable.Rows.Count);
        
        return dataTable.Rows.Count;
    }

    /// <summary>
    /// Validates connection health using the enhanced connection factory
    /// </summary>
    public async Task<bool> ValidateConnectionHealthAsync(IDbConnection connection)
    {
        return await _connectionFactory.ValidateConnectionHealthAsync(connection);
    }

    /// <summary>
    /// Gets current connection pool metrics
    /// </summary>
    public ConnectionPoolMetrics GetPoolMetrics()
    {
        return _connectionFactory.GetPoolMetrics();
    }

    /// <summary>
    /// Creates an operation scope for sharing connections across multiple operations
    /// </summary>
    public async Task<IOperationScope> CreateOperationScopeAsync()
    {
        return await _connectionFactory.CreateOperationScopeAsync(_databaseConfig);
    }

    /// <summary>
    /// Gets connection pool health status
    /// </summary>
    public async Task<ConnectionPoolHealth> GetPoolHealthAsync()
    {
        return await _connectionFactory.GetPoolHealthAsync();
    }

    /// <summary>
    /// Records performance metrics for connection operations
    /// </summary>
    public async Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics)
    {
        await _connectionFactory.RecordPerformanceMetricsAsync(metrics);
    }

    #endregion

    #region Private Helper Methods

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
        if (parameters == null)
            return;
            
        foreach (var param in parameters)
        {
            var parameterValue = param.Value ?? DBNull.Value;
            command.Parameters.AddWithValue($"@{param.Key}", parameterValue);
            _logger.LogTrace("Parameter: @{ParamName} = {ParamValue}", param.Key, parameterValue);
        }
    }

    #endregion
}