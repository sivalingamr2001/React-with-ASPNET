using System.Text.Json.Serialization;

namespace Janatics.DataEngine.Infrastructure.Models;

/// <summary>
/// Represents detailed connection pool metrics for monitoring and analytics
/// </summary>
public enum PoolStatus { Healthy, Degraded, Critical, Exhausted }

public enum LoadBalancingStrategy { RoundRobin, Random, LeastConnections, FastestResponse }

public class ConnectionPoolMetrics
{
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int TotalConnections { get; set; }
    public int MaxPoolSize { get; set; }
    public double PoolUtilizationPercentage { get; set; }
    public int PotentialLeaks { get; set; }

    // Performance Histograms
    public double AverageAcquisitionTimeMs { get; set; }
    public double P95AcquisitionTimeMs { get; set; } // Identifies the slowest 5% of requests

    // Counter Metrics (Use long for Interlocked operations)
    public long ConnectionsCreated { get; set; }
    public long ConnectionsDisposed { get; set; }
    public int PoolExhaustionCount { get; set; }
    public int ConnectionTimeouts { get; set; }
    public int ConnectionFailures { get; set; }

    // Read Replica Stats
    public int HealthyReplicaCount { get; set; }
    public double AvgReplicaLagSeconds { get; set; }
    public double ReadReplicaUtilizationPercentage { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ConnectionPoolHealth
{
    public bool IsHealthy { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PoolStatus Status { get; set; } = PoolStatus.Healthy;
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> DiagnosticSnapshots { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

public class ReadReplicaConfig
{
    public bool EnableReadReplicas { get; set; } = false;
    public List<string> ConnectionStrings { get; set; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;

    public int MaxAllowedLagSeconds { get; set; } = 5; // Disables replica if lag is too high
    public int MaxPoolSize { get; set; } = 10;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public class ConnectionPerformanceMetrics
{
    public string CorrelationId { get; set; } = string.Empty;
    public string? QueryTag { get; set; } // Helps identify the specific repo or method
    public TimeSpan AcquisitionTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int RetryCount { get; set; }
    public bool UsedReadReplica { get; set; }
    public string DatabaseProvider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
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


public class ConnectionPoolAnalytics
{
    public ConnectionPoolMetrics CurrentMetrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<ConnectionPerformanceMetrics> RecentSlowQueries { get; set; } = new();

    public void GenerateRecommendations(int acquisitionThresholdMs = 50)
    {
        Recommendations.Clear();

        if (CurrentMetrics.PoolUtilizationPercentage > 85)
            Recommendations.Add("CRITICAL: Pool nearly exhausted. Increase MaxPoolSize immediately.");

        if (CurrentMetrics.P95AcquisitionTimeMs > acquisitionThresholdMs)
            Recommendations.Add($"LATENCY: High P95 acquisition ({CurrentMetrics.P95AcquisitionTimeMs}ms). Check for connection leaks or network jitter.");

        if (CurrentMetrics.AvgReplicaLagSeconds > 10)
            Recommendations.Add("REPLICATION: High lag detected. Readers may see stale data.");

        if (CurrentMetrics.ConnectionFailures > 0)
            Recommendations.Add($"RESILIENCY: {CurrentMetrics.ConnectionFailures} failures detected. Verify DB credentials and firewall rules.");
    }
}

public class ConnectionLeakDetection
{
    public string ConnectionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RequestPath { get; set; } // e.g. "GET /api/orders/v1"
    public string? StackTrace { get; set; }
    public int CreationThreadId { get; set; }
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;
    public bool IsCriticalLeak(TimeSpan threshold) => Age > threshold;
}
