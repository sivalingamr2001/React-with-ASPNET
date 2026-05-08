using DataEngine.Infrastruture.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DataEngine.Infrastruture.ConnectionFactory;

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    protected readonly ILogger<DatabaseConnectionFactory> _logger;

    public DatabaseConnectionFactory(ILogger<DatabaseConnectionFactory>? logger = null)
    {
        _logger = logger ?? NullLogger<DatabaseConnectionFactory>.Instance;
    }

    public IDbConnection CreateConnection(DatabaseConfig config)
    {
        var connectionString = BuildConnectionString(config);

        _logger.LogDebug("Creating connection for provider: {Provider}", config.Provider);

        return config.Provider switch
        {
            DatabaseProvider.Sqlite => new SqliteConnection(connectionString),
            DatabaseProvider.PostgreSQL => new NpgsqlConnection(connectionString),
            DatabaseProvider.MySQL => new MySqlConnection(connectionString),
            DatabaseProvider.SqlServer => new SqlConnection(connectionString),
            DatabaseProvider.Oracle => new OracleConnection(connectionString),
            _ => new SqliteConnection(connectionString)
        };
    }

    private string BuildConnectionString(DatabaseConfig config)
    {
        try
        {
            return config.Provider switch
            {
                DatabaseProvider.Sqlite => BuildSqliteString(config),
                DatabaseProvider.PostgreSQL => BuildPostgreSqlString(config),
                DatabaseProvider.SqlServer => BuildSqlServerString(config),
                DatabaseProvider.Oracle => BuildOracleString(config),
                _ => BuildSqliteString(config)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build connection string for {Provider}. Using raw string.", config.Provider);
            return config.ConnectionString;
        }
    }

    private static string BuildSqlServerString(DatabaseConfig config)
    {
        var builder = new SqlConnectionStringBuilder(config.ConnectionString)
        {
            Pooling = config.EnablePooling,
            MaxPoolSize = config.MaxPoolSize,
            MinPoolSize = config.MinPoolSize,
            ConnectTimeout = config.ConnectionTimeoutSeconds
        };
        return builder.ToString();
    }

    private static string BuildOracleString(DatabaseConfig config)
    {
        var builder = new OracleConnectionStringBuilder(config.ConnectionString)
        {
            Pooling = config.EnablePooling,
            MaxPoolSize = config.MaxPoolSize,
            MinPoolSize = config.MinPoolSize,
            ConnectionTimeout = config.ConnectionTimeoutSeconds
        };
        return builder.ToString();
    }

    private static string BuildSqliteString(DatabaseConfig config)
    {
        var builder = new SqliteConnectionStringBuilder(config.ConnectionString);
        if (config.EnablePooling) builder.Cache = SqliteCacheMode.Shared;
        return builder.ToString();
    }

    private static string BuildPostgreSqlString(DatabaseConfig config)
    {
        var builder = new NpgsqlConnectionStringBuilder(config.ConnectionString)
        {
            Pooling = config.EnablePooling,
            MaxPoolSize = config.MaxPoolSize,
            MinPoolSize = config.MinPoolSize,
            ConnectionIdleLifetime = config.ConnectionIdleLifetimeMinutes * 60,
            CommandTimeout = config.CommandTimeoutSeconds,
            Timeout = config.ConnectionTimeoutSeconds
        };
        return builder.ToString();
    }

    public IOperationScope CreateOperationScope(DatabaseConfig config)
    {
        return new OperationScope(this, config, NullLogger<OperationScope>.Instance);
    }
}
