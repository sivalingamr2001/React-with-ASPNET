using System.Text.Json.Serialization;

namespace DataEngine.Infrastruture.Models;

public enum DatabaseProvider { Sqlite, PostgreSQL, SqlServer, MySQL, Oracle }

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DatabaseProvider Provider { get; set; }
    public string DefaultTimezone { get; set; } = "UTC";

    public int ConnectionIdleLifetimeMinutes { get; set; } = 15;

    // Pool Settings
    public int MaxPoolSize { get; set; } = 20;
    public int MinPoolSize { get; set; } = 5;
    public bool EnablePooling { get; set; } = true;

    // Reliability
    public int CommandTimeoutSeconds { get; set; } = 60;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan LeakDetectionThreshold { get; set; } = TimeSpan.FromMinutes(5);

    // Advanced Features
    public ReadReplicaConfig? ReadReplicaConfig { get; set; }
    public bool EnableConnectionMultiplexing { get; set; } = false;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public int MaxConcurrentOperations { get; set; } = 10;
}

public class DatabaseConfigurations
{
    public DatabaseConfig ConfigDatabase { get; set; } = new();
    public DatabaseConfig ProcessDatabase { get; set; } = new();
}