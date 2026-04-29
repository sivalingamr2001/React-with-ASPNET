namespace KATCRUDServices.Core.Models
{
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseProvider Provider { get; set; }
        public string DefaultTimezone { get; set; } = "UTC";
        
        // Connection Pool Configuration
        public int MaxPoolSize { get; set; } = 20;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionIdleLifetimeMinutes { get; set; } = 15;
        public int CommandTimeoutSeconds { get; set; } = 60;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public bool EnablePooling { get; set; } = true;
        
        // Advanced Optimization Features
        public ReadReplicaConfig? ReadReplicaConfig { get; set; }
        public bool EnableConnectionMultiplexing { get; set; } = false;
        public bool EnablePerformanceMetrics { get; set; } = true;
        public int MaxConcurrentOperations { get; set; } = 10;
    }

    public enum DatabaseProvider
    {
        PostgreSQL,
        SqlServer,
        MySQL
    }

    public class DatabaseConfigurations
    {
        public DatabaseConfig ConfigDatabase { get; set; } = new();
        public DatabaseConfig TransactionDatabase { get; set; } = new();
    }
}