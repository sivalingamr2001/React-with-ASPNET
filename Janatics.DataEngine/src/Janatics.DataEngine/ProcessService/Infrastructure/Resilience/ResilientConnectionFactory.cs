using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Janatics.DataEngine.ProcessService.Infrastructure.Resilience;

public class ResilientConnectionFactory : IResilientConnectionFactory, IDisposable
{
    private readonly ILogger<ResilientConnectionFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DatabaseConfig _defaultDatabaseConfig;
    private readonly ConcurrentQueue<ConnectionPerformanceMetrics> _performanceMetrics;
    private readonly ConcurrentDictionary<string, ConnectionOperationContext> _activeOperations;
    private readonly ConcurrentDictionary<string, DateTime> _connectionCreationTimes;
    private readonly Timer _healthCheckTimer;
    private readonly ConnectionPoolMetrics _currentMetrics;
    private readonly object _metricsLock = new();
    private bool _disposed = false;

    public ResilientConnectionFactory(
        ILogger<ResilientConnectionFactory> logger,
        ILoggerFactory loggerFactory,
        DatabaseConfig defaultDatabaseConfig)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _defaultDatabaseConfig = defaultDatabaseConfig ?? throw new ArgumentNullException(nameof(defaultDatabaseConfig));
        _performanceMetrics = new ConcurrentQueue<ConnectionPerformanceMetrics>();
        _activeOperations = new ConcurrentDictionary<string, ConnectionOperationContext>();
        _connectionCreationTimes = new ConcurrentDictionary<string, DateTime>();
        _currentMetrics = new ConnectionPoolMetrics();

        // Supervisor: Runs every 30 seconds to detect leaks and check health
        _healthCheckTimer = new Timer(PerformResiliencyCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public IDbConnection CreateConnection(DatabaseConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Provider switch
        {
            DatabaseProvider.PostgreSQL => new Npgsql.NpgsqlConnection(config.ConnectionString),
            DatabaseProvider.SqlServer => new Microsoft.Data.SqlClient.SqlConnection(config.ConnectionString),
            DatabaseProvider.MySQL => new MySql.Data.MySqlClient.MySqlConnection(config.ConnectionString),
            DatabaseProvider.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(config.ConnectionString),
            DatabaseProvider.Sqlite => new Microsoft.Data.Sqlite.SqliteConnection(config.ConnectionString),
            _ => throw new NotSupportedException($"Database provider '{config.Provider}' is not supported.")
        };
    }

    public IOperationScope CreateOperationScope(DatabaseConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new OperationScope(this, config, _loggerFactory.CreateLogger<OperationScope>());
    }

    public async Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var connection = await CreateConnectionAsync(_defaultDatabaseConfig).ConfigureAwait(false);
        return await operation(connection).ConfigureAwait(false);
    }

    public async Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var connection = await CreateConnectionAsync(_defaultDatabaseConfig).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await operation(connection, transaction).ConfigureAwait(false);
            transaction.Commit();
            return result;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // ignore rollback failures
            }

            throw;
        }
    }

    public Task<IOperationScope> CreateOperationScopeAsync()
        => Task.FromResult((IOperationScope)new OperationScope(this, _defaultDatabaseConfig, _loggerFactory.CreateLogger<OperationScope>()));

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
    {
        if (transaction != null)
            return await ExecuteNonQueryAsync(sql, parameters, transaction.Connection ?? throw new InvalidOperationException("Transaction has no active connection"), transaction).ConfigureAwait(false);

        using var connection = await CreateConnectionAsync(_defaultDatabaseConfig).ConfigureAwait(false);
        return await ExecuteNonQueryAsync(sql, parameters, connection).ConfigureAwait(false);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
    {
        if (transaction != null)
            return await ExecuteScalarAsync(sql, parameters, transaction.Connection ?? throw new InvalidOperationException("Transaction has no active connection"), transaction).ConfigureAwait(false);

        using var connection = await CreateConnectionAsync(_defaultDatabaseConfig).ConfigureAwait(false);
        return await ExecuteScalarAsync(sql, parameters, connection).ConfigureAwait(false);
    }

    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
    {
        if (transaction != null)
            return await ExecuteQueryAsync(sql, parameters, transaction.Connection ?? throw new InvalidOperationException("Transaction has no active connection"), transaction).ConfigureAwait(false);

        using var connection = await CreateConnectionAsync(_defaultDatabaseConfig).ConfigureAwait(false);
        return await ExecuteQueryAsync(sql, parameters, connection).ConfigureAwait(false);
    }

    public Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction? transaction = null)
    {
        if (transaction == null)
            throw new NotSupportedException("Bulk insert with no explicit transaction is not supported by ResilientConnectionFactory.");

        return BulkInsertAsync(tableName, dataTable, transaction.Connection ?? throw new InvalidOperationException("Transaction has no active connection"), transaction);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = PrepareCommand(connection, sql, parameters, transaction);
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        return command.ExecuteNonQuery();
    }

    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = PrepareCommand(connection, sql, parameters, transaction);
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteScalarAsync().ConfigureAwait(false);

        return command.ExecuteScalar();
    }

    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
    {
        using var command = PrepareCommand(connection, sql, parameters, transaction);
        if (command is DbCommand dbCommand)
        {
            await using var asyncReader = await dbCommand.ExecuteReaderAsync().ConfigureAwait(false);
            var result = new DataTable();
            result.Load(asyncReader);
            return result;
        }

        using var syncReader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(syncReader);
        return table;
    }

    public Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null)
    {
        throw new NotSupportedException("BulkInsert is not supported by the default resilient connection factory.");
    }

    private static IDbCommand PrepareCommand(IDbConnection connection, string sql, Dictionary<string, object> parameters, IDbTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL must be provided.", nameof(sql));

        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;

        if (parameters == null)
            return command;

        foreach (var parameterEntry in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterEntry.Key;
            parameter.Value = parameterEntry.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    /// <summary>
    /// Creates a connection with integrated performance tracking and leak detection.
    /// </summary>
    public async Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        var connection = CreateConnection(config);

        try
        {
            if (connection is System.Data.Common.DbConnection dbConn)
                await dbConn.OpenAsync();
            else
                connection.Open();

            stopwatch.Stop();

            var hashCode = connection.GetHashCode().ToString();
            var startTime = DateTime.UtcNow;

            // Register tracking
            _connectionCreationTimes.TryAdd(hashCode, startTime);
            _activeOperations.TryAdd(hashCode, new ConnectionOperationContext
            {
                ConnectionHashCode = hashCode,
                StartTime = startTime,
                OperationType = "Primary"
            });

            await RecordPerformanceMetricsAsync(new ConnectionPerformanceMetrics
            {
                AcquisitionTime = stopwatch.Elapsed,
                DatabaseProvider = config.Provider.ToString(),
                Timestamp = startTime,
                UsedReadReplica = false
            });

            // Return wrapped connection to auto-release from tracking on Dispose
            return new TrackedDbConnection(connection, () => ReleaseTracking(hashCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resiliency Layer: Failed to open connection to {Provider}", config.Provider);
            connection?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a read-only connection, balancing load across available replicas.
    /// </summary>
    public async Task<IDbConnection> CreateReadOnlyConnectionAsync(DatabaseConfig config, ReadReplicaConfig? replicaConfig = null)
    {
        var connectionString = config.ConnectionString;

        if (replicaConfig?.EnableReadReplicas == true && replicaConfig.ConnectionStrings.Count > 0)
        {
            // Resilient Strategy: Round Robin / Random selection
            int index = Random.Shared.Next(replicaConfig.ConnectionStrings.Count);
            connectionString = replicaConfig.ConnectionStrings[index];
        }

        var replicaConfigObj = new DatabaseConfig { ConnectionString = connectionString, Provider = config.Provider };
        return await CreateConnectionAsync(replicaConfigObj);
    }

    public Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics)
    {
        _performanceMetrics.Enqueue(metrics);

        // Keep a rolling window of 100 items for trend analysis
        while (_performanceMetrics.Count > 100) _performanceMetrics.TryDequeue(out _);

        return Task.CompletedTask;
    }

    private void ReleaseTracking(string hashCode)
    {
        _connectionCreationTimes.TryRemove(hashCode, out _);
        _activeOperations.TryRemove(hashCode, out _);
    }

    private void PerformResiliencyCheck(object? state)
    {
        if (_disposed) return;

        lock (_metricsLock)
        {
            try
            {
                var active = _activeOperations.Values.ToList();
                _currentMetrics.ActiveConnections = active.Count;
                _currentMetrics.PotentialLeaks = active.Count(x => x.IsPotentialLeak);
                _currentMetrics.LastUpdated = DateTime.UtcNow;

                if (_currentMetrics.PotentialLeaks > 0)
                {
                    _logger.LogCritical("Resiliency Alert: {Count} connections have been held longer than 30 mins!", _currentMetrics.PotentialLeaks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resiliency Supervisor failed during health check.");
            }
        }
    }

    public ConnectionPoolMetrics GetPoolMetrics() => _currentMetrics;

    public Task<ConnectionPoolHealth> GetPoolHealthAsync()
    {
        lock (_metricsLock)
        {
            return Task.FromResult(new ConnectionPoolHealth
            {
                // CHANGE STRINGS TO ENUM VALUES
                IsHealthy = _currentMetrics.PotentialLeaks == 0 && _currentMetrics.ActiveConnections < 100,
                Status = _currentMetrics.PotentialLeaks > 0 ? PoolStatus.Degraded : PoolStatus.Healthy,
                LastChecked = DateTime.UtcNow
            });
        }
    }

    public async Task<ConnectionPoolAnalytics> GetPoolAnalyticsAsync()
    {
        var analytics = new ConnectionPoolAnalytics { CurrentMetrics = _currentMetrics };
        analytics.GenerateRecommendations();
        return await Task.FromResult(analytics);
    }

    public Task<bool> ValidateConnectionHealthAsync(IDbConnection connection)
        => Task.FromResult(connection.State == ConnectionState.Open);

    public async Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config)
    {
        var scope = CreateOperationScope(config);
        return await Task.FromResult(scope);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _healthCheckTimer?.Dispose();
        _activeOperations.Clear();
        _connectionCreationTimes.Clear();

        _logger.LogInformation("ResilientConnectionFactory shutting down safely.");
    }
}

/// <summary>
/// Internal Proxy to intercept Dispose and notify the Factory
/// </summary>
internal class TrackedDbConnection : IDbConnection
{
    private readonly IDbConnection _inner;
    private readonly Action _onDispose;

    public TrackedDbConnection(IDbConnection inner, Action onDispose)
    {
        _inner = inner;
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        _inner.Dispose();
        _onDispose();
    }

#pragma warning disable CS8767
    public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value!; }
#pragma warning restore CS8767
    public int ConnectionTimeout => _inner.ConnectionTimeout;
    public string Database => _inner.Database;
    public ConnectionState State => _inner.State;
    public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
    public void Close() => _inner.Close();
    public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
    public IDbCommand CreateCommand() => _inner.CreateCommand();
    public void Open() => _inner.Open();
}
