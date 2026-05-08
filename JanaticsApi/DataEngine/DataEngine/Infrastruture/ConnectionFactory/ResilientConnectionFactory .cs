using DataEngine.Infrastruture.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace DataEngine.Infrastruture.ConnectionFactory;

public class ResilientConnectionFactory : DatabaseConnectionFactory, IResilientConnectionFactory, IDisposable
{
    private readonly new ILogger<ResilientConnectionFactory> _logger;
    private readonly ConcurrentQueue<ConnectionPerformanceMetrics> _performanceMetrics;
    private readonly ConcurrentDictionary<string, ConnectionOperationContext> _activeOperations;
    private readonly ConcurrentDictionary<string, DateTime> _connectionCreationTimes;
    private readonly Timer _healthCheckTimer;
    private readonly ConnectionPoolMetrics _currentMetrics;
    private readonly object _metricsLock = new();
    private bool _disposed = false;

    public ResilientConnectionFactory(ILogger<ResilientConnectionFactory> logger) : base(logger)
    {
        _logger = logger;
        _performanceMetrics = new ConcurrentQueue<ConnectionPerformanceMetrics>();
        _activeOperations = new ConcurrentDictionary<string, ConnectionOperationContext>();
        _connectionCreationTimes = new ConcurrentDictionary<string, DateTime>();
        _currentMetrics = new ConnectionPoolMetrics();

        // Supervisor: Runs every 30 seconds to detect leaks and check health
        _healthCheckTimer = new Timer(PerformResiliencyCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
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
