using System.Collections.Concurrent;
using System.Data;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;

namespace KATCRUDServices.Core.Factories;

/// <summary>
/// Enhanced connection factory with advanced optimization features including
/// connection multiplexing, read replica support, and comprehensive monitoring
/// </summary>
public class EnhancedConnectionFactory : DatabaseConnectionFactory, IEnhancedConnectionFactory, IDisposable
{
    private readonly ILogger<EnhancedConnectionFactory> _logger;
    private readonly ConcurrentQueue<ConnectionPerformanceMetrics> _performanceMetrics;
    private readonly ConcurrentDictionary<string, DateTime> _connectionCreationTimes;
    private readonly ConcurrentDictionary<string, ReadReplicaConnectionPool> _readReplicaPools;
    private readonly ConcurrentDictionary<string, ConnectionOperationContext> _activeOperations;
    private readonly Timer _metricsCollectionTimer;
    private ConnectionPoolMetrics _currentMetrics;
    private readonly object _metricsLock = new();
    private bool _disposed = false;

    public EnhancedConnectionFactory(ILogger<EnhancedConnectionFactory> logger) : base(logger)
    {
        _logger = logger;
        _performanceMetrics = new ConcurrentQueue<ConnectionPerformanceMetrics>();
        _connectionCreationTimes = new ConcurrentDictionary<string, DateTime>();
        _readReplicaPools = new ConcurrentDictionary<string, ReadReplicaConnectionPool>();
        _activeOperations = new ConcurrentDictionary<string, ConnectionOperationContext>();
        _currentMetrics = new ConnectionPoolMetrics();
        
        // Initialize metrics collection timer (runs every 30 seconds)
        _metricsCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Creates a connection asynchronously with advanced optimization features
    /// </summary>
    public async Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogDebug("Creating connection async with correlation ID: {CorrelationId}", correlationId);
            
            var connection = CreateConnection(config);
            if (connection is NpgsqlConnection npgsqlConnection)
            {
                await npgsqlConnection.OpenAsync();
            }
            else
            {
                connection.Open();
            }
            
            // Track connection creation
            var connectionHashCode = connection.GetHashCode().ToString();
            _connectionCreationTimes.TryAdd(connectionHashCode, DateTime.UtcNow);
            
            stopwatch.Stop();
            
            // Record performance metrics
            await RecordPerformanceMetricsAsync(new ConnectionPerformanceMetrics
            {
                ConnectionAcquisitionTime = stopwatch.Elapsed,
                DatabaseProvider = config.Provider.ToString(),
                Timestamp = DateTime.UtcNow,
                UsedReadReplica = false
            });
            
            _logger.LogDebug("Connection created successfully with correlation ID: {CorrelationId}, HashCode: {HashCode}, Time: {ElapsedMs}ms", 
                correlationId, connectionHashCode, stopwatch.ElapsedMilliseconds);
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create connection async with correlation ID: {CorrelationId}", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Creates an operation scope asynchronously for connection sharing
    /// </summary>
    public async Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config)
    {
        _logger.LogDebug("Creating operation scope async");
        
        var operationScope = CreateOperationScope(config);
        await operationScope.EnsureConnectionOpenAsync();
        
        return operationScope;
    }

    /// <summary>
    /// Gets current connection pool metrics
    /// </summary>
    public ConnectionPoolMetrics GetPoolMetrics()
    {
        lock (_metricsLock)
        {
            return new ConnectionPoolMetrics
            {
                ActiveConnections = _currentMetrics.ActiveConnections,
                IdleConnections = _currentMetrics.IdleConnections,
                TotalConnections = _currentMetrics.TotalConnections,
                MaxPoolSize = _currentMetrics.MaxPoolSize,
                PoolUtilizationPercentage = _currentMetrics.PoolUtilizationPercentage,
                ConnectionsCreatedLastMinute = _currentMetrics.ConnectionsCreatedLastMinute,
                ConnectionsDisposedLastMinute = _currentMetrics.ConnectionsDisposedLastMinute,
                AverageConnectionAge = _currentMetrics.AverageConnectionAge,
                PotentialLeaks = _currentMetrics.PotentialLeaks,
                LastUpdated = DateTime.UtcNow,
                AverageConnectionAcquisitionTimeMs = _currentMetrics.AverageConnectionAcquisitionTimeMs,
                ConnectionTimeouts = _currentMetrics.ConnectionTimeouts,
                ConnectionFailures = _currentMetrics.ConnectionFailures,
                ReadReplicaConnections = _currentMetrics.ReadReplicaConnections,
                PrimaryConnections = _currentMetrics.PrimaryConnections,
                ReadReplicaUtilizationPercentage = _currentMetrics.ReadReplicaUtilizationPercentage
            };
        }
    }

    /// <summary>
    /// Validates connection health asynchronously
    /// </summary>
    public async Task<bool> ValidateConnectionHealthAsync(IDbConnection connection)
    {
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                return false;
            }

            // Perform a simple health check query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // 5 second timeout for health check
            
            var result = command.ExecuteScalar();
            return result != null && result.ToString() == "1";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection health validation failed for connection {ConnectionHashCode}", 
                connection.GetHashCode());
            return false;
        }
    }

    /// <summary>
    /// Creates a read-only connection for read operations (may use read replica)
    /// </summary>
    public async Task<IDbConnection> CreateReadOnlyConnectionAsync(DatabaseConfig config, ReadReplicaConfig? readReplicaConfig = null)
    {
        var effectiveReadReplicaConfig = readReplicaConfig ?? config.ReadReplicaConfig;
        
        if (effectiveReadReplicaConfig?.EnableReadReplicas == true && 
            effectiveReadReplicaConfig.ReadReplicaConnectionStrings.Any())
        {
            return await CreateReadReplicaConnectionAsync(config, effectiveReadReplicaConfig);
        }
        
        // Fall back to primary connection
        return await CreateConnectionAsync(config);
    }

    /// <summary>
    /// Gets connection pool analytics and recommendations
    /// </summary>
    public async Task<ConnectionPoolAnalytics> GetPoolAnalyticsAsync()
    {
        var analytics = new ConnectionPoolAnalytics
        {
            CurrentMetrics = GetPoolMetrics(),
            RecentPerformanceData = GetRecentPerformanceMetrics()
        };
        
        analytics.GenerateRecommendations();
        
        return analytics;
    }

    /// <summary>
    /// Records performance metrics for a connection operation
    /// </summary>
    public async Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics)
    {
        _performanceMetrics.Enqueue(metrics);
        
        // Keep only recent metrics (last 1000 entries)
        while (_performanceMetrics.Count > 1000)
        {
            _performanceMetrics.TryDequeue(out _);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets connection pool health status
    /// </summary>
    public async Task<ConnectionPoolHealth> GetPoolHealthAsync()
    {
        var metrics = GetPoolMetrics();
        var health = new ConnectionPoolHealth
        {
            LastChecked = DateTime.UtcNow
        };

        // Determine health status
        if (metrics.PoolUtilizationPercentage > 90)
        {
            health.IsHealthy = false;
            health.Status = "Critical";
            health.Issues.Add("Connection pool utilization is critically high");
        }
        else if (metrics.PoolUtilizationPercentage > 75)
        {
            health.IsHealthy = true;
            health.Status = "Warning";
            health.Issues.Add("Connection pool utilization is high");
        }
        else
        {
            health.IsHealthy = true;
            health.Status = "Healthy";
        }

        if (metrics.PotentialLeaks > 0)
        {
            health.Issues.Add($"Potential connection leaks detected: {metrics.PotentialLeaks}");
        }

        if (metrics.ConnectionFailures > 0)
        {
            health.Issues.Add($"Connection failures in last period: {metrics.ConnectionFailures}");
        }

        // Generate recommendations
        if (metrics.PoolUtilizationPercentage > 80)
        {
            health.Recommendations.Add("Consider increasing MaxPoolSize");
        }

        if (metrics.AverageConnectionAcquisitionTimeMs > 100)
        {
            health.Recommendations.Add("Connection acquisition time is high - consider optimizing queries or increasing pool size");
        }

        return health;
    }

    private async Task<IDbConnection> CreateReadReplicaConnectionAsync(DatabaseConfig config, ReadReplicaConfig readReplicaConfig)
    {
        var connectionString = SelectReadReplicaConnectionString(readReplicaConfig);
        var readReplicaDbConfig = new DatabaseConfig
        {
            ConnectionString = connectionString,
            Provider = config.Provider,
            MaxPoolSize = readReplicaConfig.ReadReplicaMaxPoolSize,
            MinPoolSize = readReplicaConfig.ReadReplicaMinPoolSize,
            EnablePooling = config.EnablePooling,
            CommandTimeoutSeconds = config.CommandTimeoutSeconds,
            ConnectionTimeoutSeconds = config.ConnectionTimeoutSeconds
        };

        var connection = await CreateConnectionAsync(readReplicaDbConfig);
        
        // Record that this was a read replica connection
        await RecordPerformanceMetricsAsync(new ConnectionPerformanceMetrics
        {
            UsedReadReplica = true,
            DatabaseProvider = config.Provider.ToString(),
            Timestamp = DateTime.UtcNow
        });

        return connection;
    }

    private string SelectReadReplicaConnectionString(ReadReplicaConfig config)
    {
        return config.LoadBalancingStrategy.ToLower() switch
        {
            "random" => config.ReadReplicaConnectionStrings[Random.Shared.Next(config.ReadReplicaConnectionStrings.Count)],
            "roundrobin" => SelectRoundRobinReplica(config.ReadReplicaConnectionStrings),
            _ => config.ReadReplicaConnectionStrings.First()
        };
    }

    private string SelectRoundRobinReplica(List<string> connectionStrings)
    {
        // Simple round-robin implementation
        var index = Environment.TickCount % connectionStrings.Count;
        return connectionStrings[index];
    }

    private List<ConnectionPerformanceMetrics> GetRecentPerformanceMetrics()
    {
        return _performanceMetrics.ToList().TakeLast(100).ToList();
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            lock (_metricsLock)
            {
                // Update metrics based on current state
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Count connections created/disposed in last minute
                var recentConnections = _connectionCreationTimes.Values.Count(t => t >= oneMinuteAgo);
                
                // Detect potential leaks (connections older than 30 minutes)
                var potentialLeaks = _connectionCreationTimes.Values.Count(t => now - t > TimeSpan.FromMinutes(30));

                // Calculate average acquisition time from recent metrics
                var recentMetrics = _performanceMetrics.Where(m => m.Timestamp >= oneMinuteAgo).ToList();
                var avgAcquisitionTime = recentMetrics.Any() 
                    ? recentMetrics.Average(m => m.ConnectionAcquisitionTime.TotalMilliseconds) 
                    : 0;

                _currentMetrics.ConnectionsCreatedLastMinute = recentConnections;
                _currentMetrics.PotentialLeaks = potentialLeaks;
                _currentMetrics.AverageConnectionAcquisitionTimeMs = avgAcquisitionTime;
                _currentMetrics.LastUpdated = now;

                // Clean up old connection tracking data
                var cutoffTime = now.AddHours(-1);
                var keysToRemove = _connectionCreationTimes
                    .Where(kvp => kvp.Value < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _connectionCreationTimes.TryRemove(key, out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting connection metrics");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _metricsCollectionTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Simple read replica connection pool for tracking
/// </summary>
internal class ReadReplicaConnectionPool
{
    public string ConnectionString { get; set; } = string.Empty;
    public int ActiveConnections { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsHealthy { get; set; } = true;
}