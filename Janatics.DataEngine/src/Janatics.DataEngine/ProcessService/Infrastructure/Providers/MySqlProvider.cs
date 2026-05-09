// MySqlProvider.cs
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Core.Auditing;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace Janatics.DataEngine.ProcessService.Infrastructure.Providers;

/// <summary>
/// Enhanced MySQL provider that implements IDataProvider with optimized connection management,
/// connection sharing, and advanced monitoring capabilities
/// </summary>
public class MySqlProvider : IDataProvider
{
    private readonly IResilientConnectionFactory _connectionFactory;
    private readonly DatabaseConfig _databaseConfig;
    private readonly ILogger<MySqlProvider> _logger;

    public MySqlProvider(
        IResilientConnectionFactory connectionFactory,
        DatabaseConfig databaseConfig,
        ILogger<MySqlProvider> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IDataProvider Implementation (Legacy Methods)

    public async Task<IDbConnection> GetConnectionAsync()
    {
        return await _connectionFactory.CreateConnectionAsync(_databaseConfig);
    }

    public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
    {
        var transaction = connection.BeginTransaction();
        _logger.LogDebug("MySQL transaction started");
        return await Task.FromResult(transaction);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
    {
        using var command = new MySql.Data.MySqlClient.MySqlCommand(sql, GetMySqlConnection(transaction), GetMySqlTransaction(transaction));
        AddParameters(command, parameters);

        _logger.LogDebug("Executing SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Count);

        var result = await command.ExecuteNonQueryAsync();
        _logger.LogDebug("Rows affected: {RowsAffected}", result);

        return result;
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, DbCommand command)
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
        using var command = new MySqlCommand(sql, GetMySqlConnection(transaction), GetMySqlTransaction(transaction));
        AddParameters(command, parameters);

        _logger.LogDebug("Executing scalar SQL: {Sql}", sql);
        return await command.ExecuteScalarAsync();
    }

    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
    {
        var connection = transaction?.Connection ?? await GetConnectionAsync();
        using var command = new MySqlCommand(sql, (MySqlConnection)connection, (MySqlTransaction?)transaction);
        AddParameters(command, parameters);

        _logger.LogDebug("Executing query: {Sql}", sql);

        var dataTable = new DataTable();
        using var adapter = new MySqlDataAdapter(command);
        adapter.Fill(dataTable);

        if (transaction == null)
            connection.Close();

        _logger.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    public async Task<object?> ExecuteInsertWithReturnAsync(string tableName, Dictionary<string, object> parameters, string returnColumn, IDbTransaction transaction)
    {
        var formattedTableName = FormatTableName(tableName);

        var (processedColumns, processedPlaceholders, processedParameters) = ProcessParametersForSequences(parameters);

        var quotedColumns = processedColumns
            .Select(col => $"`{col}`")
            .ToList();

        var sql =
            $"INSERT INTO {formattedTableName} ({string.Join(", ", quotedColumns)}) " +
            $"VALUES ({string.Join(", ", processedPlaceholders)}); SELECT LAST_INSERT_ID();";

        _logger.LogDebug("Executing INSERT with LAST_INSERT_ID: {Sql}", sql);
        return await ExecuteScalarAsync(sql, processedParameters, transaction);
    }

    public async Task<int> ExecuteUpdateAsync(string tableName, Dictionary<string, object> parameters, Dictionary<string, object> whereConditions, IDbTransaction transaction)
    {
        var formattedTableName = FormatTableName(tableName);

        var setClauses = new List<string>();
        var allParameters = new Dictionary<string, object>();

        foreach (var param in parameters)
        {
            if (param.Value is string stringValue && stringValue.StartsWith("NEXTVAL("))
            {
                setClauses.Add($"`{param.Key}` = {stringValue}");
                _logger.LogTrace("Using sequence function in UPDATE: {SequenceFunction} for column {Column}", stringValue, param.Key);
            }
            else if (param.Value is string stringValue1 && stringValue1.StartsWith("SQL:"))
            {
                var rawSql = stringValue1.Substring(4);
                setClauses.Add($"`{param.Key}` = {rawSql}");
                _logger.LogTrace("Using raw SQL expression in UPDATE: {RawSql} for column {Column}", rawSql, param.Key);
            }
            else
            {
                setClauses.Add($"`{param.Key}` = {GetParameterPlaceholder($"set_{param.Key}")}");
                allParameters[$"set_{param.Key}"] = param.Value;
            }
        }

        var whereClause = string.Join(" AND ", whereConditions.Keys.Select(key => $"`{key}` = {GetParameterPlaceholder($"where_{key}")}"));

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
        var whereClause = string.Join(" AND ", whereConditions.Keys.Select(key => $"`{key}` = {GetParameterPlaceholder(key)}"));
        var sql = $"DELETE FROM {formattedTableName} WHERE {whereClause}";

        _logger.LogDebug("Executing DELETE: {Sql}", sql);
        return await ExecuteNonQueryAsync(sql, whereConditions, transaction);
    }

    public async Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction transaction)
    {
        var formattedTableName = FormatTableName(tableName);
        var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"`{c.ColumnName}`"));
        var parameterNames = dataTable.Columns.Cast<DataColumn>().Select(c => GetParameterPlaceholder(c.ColumnName));

        var insertSql = $"INSERT INTO {formattedTableName} ({columnNames}) VALUES ({string.Join(", ", parameterNames)})";

        _logger.LogInformation("Starting MySQL bulk insert for table {TableName} with {RowCount} rows", tableName, dataTable.Rows.Count);

        var connection = GetMySqlConnection(transaction);
        using var command = new MySqlCommand(insertSql, connection, GetMySqlTransaction(transaction));

        var parameters = new List<MySqlParameter>();
        foreach (DataColumn column in dataTable.Columns)
        {
            var param = new MySqlParameter(GetParameterPlaceholder(column.ColumnName), null);
            command.Parameters.Add(param);
            parameters.Add(param);
        }

        int rowCount = 0;
        foreach (DataRow row in dataTable.Rows)
        {
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var value = row[i];
                parameters[i].Value = value == null || value == DBNull.Value ? DBNull.Value : value;
            }

            rowCount += await command.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("MySQL bulk insert completed for table {TableName}, {RowCount} rows inserted", tableName, rowCount);

        return rowCount;
    }

    public string GetParameterPlaceholder(string parameterName) => $"@{parameterName}";

    public string FormatTableName(string tableName)
    {
        var parts = tableName.Split('.');
        var quotedParts = parts.Select(p => $"`{p}`");
        return string.Join(".", quotedParts);
    }

    #endregion

    #region IEnhancedDataProvider Implementation

    public async Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation)
    {
        await using var scope = await CreateOperationScopeAsync();
        return await operation(scope.Connection);
    }

    public async Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
    {
        await using var scope = await CreateOperationScopeAsync();
        using var transaction = await BeginTransactionAsync(scope.Connection);
        try
        {
            var result = await operation(scope.Connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            await TerminateOtherIdleConnectionsAsync(scope.Connection);
        }
    }

    private async Task TerminateOtherIdleConnectionsAsync(IDbConnection connection)
    {
        if (connection is not MySqlConnection mySqlConnection || mySqlConnection.State != ConnectionState.Open)
            return;

        try
        {
            const string sql = @"
                SELECT ID 
                FROM information_schema.PROCESSLIST 
                WHERE COMMAND = 'Sleep' 
                AND ID <> CONNECTION_ID() 
                AND DB = DATABASE()
                AND TIME > 60;";

            using var command = new MySqlCommand(sql, mySqlConnection) { CommandTimeout = 5 };
            var connectionIds = new List<ulong>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    connectionIds.Add((ulong)reader.GetInt64(0));
                }
            }

            foreach (var connId in connectionIds)
            {
                using var killCmd = new MySqlCommand($"KILL {connId};", mySqlConnection);
                await killCmd.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException ex) when (ex.Number == 1094)
        {
            _logger.LogDebug("Cannot kill own connection in idle connection cleanup.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while terminating idle MySQL connections");
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new MySqlCommand(sql, (MySqlConnection)connection, (MySqlTransaction?)transaction);
        AddParameters(command, parameters);
        _logger.LogDebug("Executing SQL with shared connection: {Sql}", sql);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new MySqlCommand(sql, (MySqlConnection)connection, (MySqlTransaction?)transaction);
        AddParameters(command, parameters);
        _logger.LogDebug("Executing scalar SQL with shared connection: {Sql}", sql);
        return await command.ExecuteScalarAsync();
    }

    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = new MySqlCommand(sql, (MySqlConnection)connection, (MySqlTransaction?)transaction);
        AddParameters(command, parameters);
        _logger.LogDebug("Executing query with shared connection: {Sql}", sql);

        var dataTable = new DataTable();
        using var adapter = new MySqlDataAdapter(command);
        adapter.Fill(dataTable);
        _logger.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
        return dataTable;
    }

    public async Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null)
    {
        var formattedTableName = FormatTableName(tableName);
        var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"`{c.ColumnName}`"));
        var parameterNames = dataTable.Columns.Cast<DataColumn>().Select(c => GetParameterPlaceholder(c.ColumnName));

        var insertSql = $"INSERT INTO {formattedTableName} ({columnNames}) VALUES ({string.Join(", ", parameterNames)})";

        _logger.LogInformation("Starting MySQL bulk insert with shared connection for table {TableName}", tableName);

        using var command = new MySqlCommand(insertSql, (MySqlConnection)connection, (MySqlTransaction?)transaction);

        var parameters = new List<MySqlParameter>();
        foreach (DataColumn column in dataTable.Columns)
        {
            var param = new MySqlParameter(GetParameterPlaceholder(column.ColumnName), null);
            command.Parameters.Add(param);
            parameters.Add(param);
        }

        int rowCount = 0;
        foreach (DataRow row in dataTable.Rows)
        {
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var value = row[i];
                parameters[i].Value = value == null || value == DBNull.Value ? DBNull.Value : value;
            }

            rowCount += await command.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("MySQL bulk insert completed for table {TableName}, {RowCount} rows inserted", tableName, rowCount);
        return rowCount;
    }

    public async Task<bool> ValidateConnectionHealthAsync(IDbConnection connection)
    {
        return await _connectionFactory.ValidateConnectionHealthAsync(connection);
    }

    public ConnectionPoolMetrics GetPoolMetrics()
    {
        return _connectionFactory.GetPoolMetrics();
    }

    public async Task<IOperationScope> CreateOperationScopeAsync()
    {
        return await _connectionFactory.CreateOperationScopeAsync(_databaseConfig);
    }

    public async Task<ConnectionPoolHealth> GetPoolHealthAsync()
    {
        return await _connectionFactory.GetPoolHealthAsync();
    }

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
                    placeholders.Add(stringValue);
                    continue;
                }
                if (stringValue.StartsWith("SQL:"))
                {
                    placeholders.Add(stringValue.Substring(4));
                    continue;
                }
            }

            placeholders.Add(GetParameterPlaceholder(param.Key));
            parameters[param.Key] = param.Value;
        }

        return (columns, placeholders, parameters);
    }

    private void AddParameters(MySqlCommand command, Dictionary<string, object> parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            var parameterValue = param.Value ?? DBNull.Value;
            command.Parameters.AddWithValue($"@{param.Key}", parameterValue);
            _logger.LogTrace("Parameter: @{ParamName} = {ParamValue}", param.Key, parameterValue);
        }
    }

    private static MySqlConnection GetMySqlConnection(IDbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return transaction.Connection as MySqlConnection
            ?? throw new InvalidOperationException("The MySQL provider requires an active MySqlConnection on the supplied transaction.");
    }

    private static MySqlTransaction GetMySqlTransaction(IDbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return transaction as MySqlTransaction
            ?? throw new InvalidOperationException("The MySQL provider requires a MySqlTransaction instance.");
    }

    #endregion
}