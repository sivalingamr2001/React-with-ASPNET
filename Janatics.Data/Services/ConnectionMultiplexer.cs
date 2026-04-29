using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Services;

/// <summary>
/// Connection multiplexer that enables sharing connections across multiple CRUD operations
/// to reduce connection overhead and improve performance
/// </summary>
public class ConnectionMultiplexer : IConnectionMultiplexer
{
    private readonly IEnhancedConnectionFactory _connectionFactory;
    private readonly ILogger<ConnectionMultiplexer> _logger;
    private readonly ConcurrentDictionary<string, IDbConnection> _activeConnections;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ConnectionMultiplexerMetrics _metrics;
    private readonly object _metricsLock = new();
    private bool _disposed = false;

    public ConnectionMultiplexer(
        IEnhancedConnectionFactory connectionFactory,
        ILogger<ConnectionMultiplexer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _activeConnections = new ConcurrentDictionary<string, IDbConnection>();
        _connectionSemaphore = new SemaphoreSlim(10, 10); // Max 10 concurrent operations
        _metrics = new ConnectionMultiplexerMetrics
        {
            LastReset = DateTime.UtcNow
        };
    }

    public async Task<T> ExecuteMultiplexedAsync<T>(
        DatabaseConfig config,
        Func<IDbConnection, Task<T>> operation,
        bool useReadReplica = true)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionMultiplexer));

        var stopwatch = Stopwatch.StartNew();
        var connectionKey = GenerateConnectionKey(config, useReadReplica);
        
        await _connectionSemaphore.WaitAsync();
        
        try
        {
            // Try to get existing connection or create new one
            var connection = await GetOrCreateConnectionAsync(connectionKey, config, useReadReplica);
            
            _logger.LogDebug("Executing multiplexed operation using connection {ConnectionKey}", connectionKey);
            
            var result = await operation(connection);
            
            UpdateMetrics(stopwatch.Elapsed, true);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing multiplexed operation");
            
            // Remove potentially bad connection
            if (_activeConnections.TryRemove(connectionKey, out var badConnection))
            {
                try
                {
                    badConnection.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            
            throw;
        }
        finally
        {
            _connectionSemaphore.Release();
            stopwatch.Stop();
        }
    }

    public async Task<IEnumerable<T>> ExecuteParallelAsync<T>(
        DatabaseConfig config,
        IEnumerable<Func<IDbConnection, Task<T>>> operations,
        bool useReadReplica = true,
        int maxConcurrency = 4)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionMultiplexer));

        var operationsList = operations.ToList();
        if (!operationsList.Any())
            return Enumerable.Empty<T>();

        _logger.LogDebug("Executing {OperationCount} operations in parallel with max concurrency {MaxConcurrency}", 
            operationsList.Count, maxConcurrency);

        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = operationsList.Select(async operation =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await ExecuteMultiplexedAsync(config, operation, useReadReplica);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        
        UpdateMetrics(TimeSpan.Zero, false, operationsList.Count);
        
        return results;
    }

    public IDbConnection? GetCurrentConnection()
    {
        return _activeConnections.Values.FirstOrDefault();
    }

    public ConnectionMultiplexerMetrics GetMetrics()
    {
        lock (_metricsLock)
        {
            return new ConnectionMultiplexerMetrics
            {
                TotalOperations = _metrics.TotalOperations,
                MultiplexedOperations = _metrics.MultiplexedOperations,
                ParallelOperations = _metrics.ParallelOperations,
                AverageOperationTimeMs = _metrics.AverageOperationTimeMs,
                ConnectionsReused = _metrics.ConnectionsReused,
                ConnectionsSaved = _metrics.ConnectionsSaved,
                LastReset = _metrics.LastReset
            };
        }
    }

    private async Task<IDbConnection> GetOrCreateConnectionAsync(
        string connectionKey, 
        DatabaseConfig config, 
        bool useReadReplica)
    {
        // Try to get existing connection
        if (_activeConnections.TryGetValue(connectionKey, out var existingConnection))
        {
            // Validate connection is still healthy
            if (existingConnection.State == ConnectionState.Open)
            {
                var isHealthy = await _connectionFactory.ValidateConnectionHealthAsync(existingConnection);
                if (isHealthy)
                {
                    lock (_metricsLock)
                    {
                        _metrics.ConnectionsReused++;
                    }
                    
                    _logger.LogDebug("Reusing existing connection {ConnectionKey}", connectionKey);
                    return existingConnection;
                }
            }
            
            // Remove unhealthy connection
            _activeConnections.TryRemove(connectionKey, out _);
            try
            {
                existingConnection.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        // Create new connection
        IDbConnection newConnection;
        if (useReadReplica)
        {
            // Try to create read replica connection, fallback to primary if needed
            newConnection = await _connectionFactory.CreateReadOnlyConnectionAsync(config);
        }
        else
        {
            newConnection = await _connectionFactory.CreateConnectionAsync(config);
        }

        // Store the new connection
        _activeConnections.TryAdd(connectionKey, newConnection);
        
        _logger.LogDebug("Created new connection {ConnectionKey}", connectionKey);
        
        return newConnection;
    }

    private string GenerateConnectionKey(DatabaseConfig config, bool useReadReplica)
    {
        // Generate a key based on connection string hash and read replica flag
        var connectionStringHash = config.ConnectionString.GetHashCode();
        return $"{connectionStringHash}_{useReadReplica}_{config.Provider}";
    }

    private void UpdateMetrics(TimeSpan operationTime, bool isMultiplexed, int parallelOperations = 0)
    {
        lock (_metricsLock)
        {
            _metrics.TotalOperations++;
            
            if (isMultiplexed)
            {
                _metrics.MultiplexedOperations++;
            }
            
            if (parallelOperations > 0)
            {
                _metrics.ParallelOperations += parallelOperations;
                // Each parallel operation saves connections (n operations = n-1 connections saved)
                _metrics.ConnectionsSaved += Math.Max(0, parallelOperations - 1);
            }
            
            // Update average operation time
            if (operationTime > TimeSpan.Zero)
            {
                var currentAverage = _metrics.AverageOperationTimeMs;
                var newAverage = (currentAverage * (_metrics.TotalOperations - 1) + operationTime.TotalMilliseconds) / _metrics.TotalOperations;
                _metrics.AverageOperationTimeMs = newAverage;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogDebug("Disposing connection multiplexer with {ConnectionCount} active connections", 
            _activeConnections.Count);

        // Dispose all active connections
        var disposalTasks = _activeConnections.Values.Select(async connection =>
        {
            try
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing connection during multiplexer cleanup");
            }
        });

        await Task.WhenAll(disposalTasks);
        
        _activeConnections.Clear();
        _connectionSemaphore.Dispose();

        var finalMetrics = GetMetrics();
        _logger.LogInformation("Connection multiplexer disposed. Final metrics: " +
            "Total operations: {TotalOps}, Multiplexed: {MultiplexedOps}, " +
            "Connections reused: {Reused}, Connections saved: {Saved}",
            finalMetrics.TotalOperations, finalMetrics.MultiplexedOperations,
            finalMetrics.ConnectionsReused, finalMetrics.ConnectionsSaved);
    }
}