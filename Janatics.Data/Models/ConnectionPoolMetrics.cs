using System;

namespace KATCRUDServices.Core.Models;

/// <summary>
/// Represents detailed connection pool metrics for monitoring and analytics
/// </summary>
public class ConnectionPoolMetrics
{
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int TotalConnections { get; set; }
    public int MaxPoolSize { get; set; }
    public double PoolUtilizationPercentage { get; set; }
    public int ConnectionsCreatedLastMinute { get; set; }
    public int ConnectionsDisposedLastMinute { get; set; }
    public TimeSpan AverageConnectionAge { get; set; }
    public int PotentialLeaks { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Performance metrics
    public double AverageConnectionAcquisitionTimeMs { get; set; }
    public int ConnectionTimeouts { get; set; }
    public int ConnectionFailures { get; set; }
    
    // Read replica metrics
    public int ReadReplicaConnections { get; set; }
    public int PrimaryConnections { get; set; }
    public double ReadReplicaUtilizationPercentage { get; set; }
}

/// <summary>
/// Represents connection pool health status
/// </summary>
public class ConnectionPoolHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Configuration for read replica support
/// </summary>
public class ReadReplicaConfig
{
    public bool EnableReadReplicas { get; set; } = false;
    public List<string> ReadReplicaConnectionStrings { get; set; } = new();
    public string LoadBalancingStrategy { get; set; } = "RoundRobin"; // RoundRobin, Random, LeastConnections
    public int ReadReplicaMaxPoolSize { get; set; } = 10;
    public int ReadReplicaMinPoolSize { get; set; } = 2;
    public TimeSpan ReadReplicaHealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Performance metrics for connection operations
/// </summary>
public class ConnectionPerformanceMetrics
{
    public TimeSpan ConnectionAcquisitionTime { get; set; }
    public TimeSpan QueryExecutionTime { get; set; }
    public int ConnectionRetries { get; set; }
    public bool UsedReadReplica { get; set; }
    public string DatabaseProvider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Analytics and recommendations for connection pool optimization
/// </summary>
public class ConnectionPoolAnalytics
{
    public ConnectionPoolMetrics CurrentMetrics { get; set; } = new();
    public List<string> OptimizationRecommendations { get; set; } = new();
    public Dictionary<string, double> PerformanceTrends { get; set; } = new();
    public List<ConnectionPerformanceMetrics> RecentPerformanceData { get; set; } = new();
    
    /// <summary>
    /// Generates optimization recommendations based on current metrics
    /// </summary>
    public void GenerateRecommendations()
    {
        OptimizationRecommendations.Clear();
        
        // Pool size recommendations
        if (CurrentMetrics.PoolUtilizationPercentage > 80)
        {
            OptimizationRecommendations.Add("Consider increasing MaxPoolSize - current utilization is high");
        }
        else if (CurrentMetrics.PoolUtilizationPercentage < 20)
        {
            OptimizationRecommendations.Add("Consider decreasing MaxPoolSize - current utilization is low");
        }
        
        // Connection leak detection
        if (CurrentMetrics.PotentialLeaks > 0)
        {
            OptimizationRecommendations.Add($"Potential connection leaks detected: {CurrentMetrics.PotentialLeaks} connections");
        }
        
        // Performance recommendations
        if (CurrentMetrics.AverageConnectionAcquisitionTimeMs > 100)
        {
            OptimizationRecommendations.Add("Connection acquisition time is high - consider connection multiplexing");
        }
        
        // Read replica recommendations
        if (CurrentMetrics.ReadReplicaUtilizationPercentage < 30 && CurrentMetrics.ReadReplicaConnections > 0)
        {
            OptimizationRecommendations.Add("Read replica utilization is low - consider routing more read operations to replicas");
        }
    }
}

/// <summary>
/// Represents connection operation tracking information
/// </summary>
public class ConnectionOperationContext
{
    /// <summary>
    /// Unique identifier for this operation
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of operation (Insert, Update, Delete, Select, etc.)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Entity or table name being operated on
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// When the operation started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Connection hash code for tracking
    /// </summary>
    public string ConnectionHashCode { get; set; } = string.Empty;

    /// <summary>
    /// Thread ID where operation is running
    /// </summary>
    public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// How long the operation has been running
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - StartTime;

    /// <summary>
    /// Whether this operation might be a leak (running too long)
    /// </summary>
    public bool IsPotentialLeak => Duration > TimeSpan.FromMinutes(30);
}

/// <summary>
/// Represents a detected connection leak
/// </summary>
public class ConnectionLeakDetection
{
    /// <summary>
    /// Connection identifier
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// When the connection was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Operation context when leak was detected
    /// </summary>
    public string OperationContext { get; set; } = string.Empty;

    /// <summary>
    /// How long the connection has been held
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Whether this is considered a leak
    /// </summary>
    public bool IsPotentialLeak => Age > TimeSpan.FromMinutes(30);

    /// <summary>
    /// Stack trace when connection was created (if available)
    /// </summary>
    public string? CreationStackTrace { get; set; }

    /// <summary>
    /// Thread ID where connection was created
    /// </summary>
    public int CreationThreadId { get; set; }

    /// <summary>
    /// Additional diagnostic information
    /// </summary>
    public Dictionary<string, object> DiagnosticData { get; set; } = new();
}

/// <summary>
/// Metrics for connection multiplexer operations
/// </summary>
public class ConnectionMultiplexerMetrics
{
    public int TotalOperations { get; set; }
    public int MultiplexedOperations { get; set; }
    public int ParallelOperations { get; set; }
    public double AverageOperationTimeMs { get; set; }
    public int ConnectionsReused { get; set; }
    public int ConnectionsSaved { get; set; }
    public DateTime LastReset { get; set; }
}