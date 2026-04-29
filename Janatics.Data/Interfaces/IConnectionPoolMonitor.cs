using KATCRUDServices.Core.Models;

using CrudConnectionPoolHealth = KATCRUDServices.Core.Models.ConnectionPoolHealth;

namespace KATCRUDServices.Core.Interfaces
{
    /// <summary>
    /// Interface for monitoring connection pool health and performance
    /// </summary>
    public interface IConnectionPoolMonitor : IDisposable
    {
        /// <summary>
        /// Logs current connection pool metrics
        /// </summary>
        Task LogPoolMetricsAsync();

        /// <summary>
        /// Gets the current health status of the connection pool
        /// </summary>
        /// <returns>Connection pool health information</returns>
        Task<CrudConnectionPoolHealth> GetPoolHealthAsync();

        /// <summary>
        /// Gets performance trends over the specified time window
        /// </summary>
        /// <param name="timeWindow">Time window for trend analysis</param>
        /// <returns>Dictionary of performance trends</returns>
        Dictionary<string, List<double>> GetPerformanceTrends(TimeSpan timeWindow);

        /// <summary>
        /// Generates optimization recommendations based on current metrics and trends
        /// </summary>
        /// <returns>List of optimization recommendations</returns>
        List<string> GenerateOptimizationRecommendations();

        /// <summary>
        /// Event raised when a connection leak is detected
        /// </summary>
        event EventHandler<ConnectionLeakDetectedEventArgs> ConnectionLeakDetected;

        /// <summary>
        /// Event raised when pool exhaustion warning is triggered
        /// </summary>
        event EventHandler<PoolExhaustionWarningEventArgs> PoolExhaustionWarning;
    }

    /// <summary>
    /// Event arguments for connection leak detection
    /// </summary>
    public class ConnectionLeakDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Number of leaked connections detected
        /// </summary>
        public int LeakCount { get; set; }

        /// <summary>
        /// Timestamp when leak was detected
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Details about the leaked connections
        /// </summary>
        public List<string> LeakDetails { get; set; } = new();

        /// <summary>
        /// Current connection pool metrics at time of detection
        /// </summary>
        public CrudConnectionPoolHealth PoolHealth { get; set; } = new();
    }

    /// <summary>
    /// Event arguments for pool exhaustion warning
    /// </summary>
    public class PoolExhaustionWarningEventArgs : EventArgs
    {
        /// <summary>
        /// Current pool utilization percentage
        /// </summary>
        public double UtilizationPercentage { get; set; }

        /// <summary>
        /// Maximum pool size
        /// </summary>
        public int MaxPoolSize { get; set; }

        /// <summary>
        /// Current active connections
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Timestamp when warning was triggered
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current connection pool health
        /// </summary>
        public CrudConnectionPoolHealth PoolHealth { get; set; } = new();
    }
}