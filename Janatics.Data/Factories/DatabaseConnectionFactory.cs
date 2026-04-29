using System.Data;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using KATCRUDServices.Core.Services;
using Npgsql;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Factories;

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly ILogger<DatabaseConnectionFactory>? _logger;

    public DatabaseConnectionFactory(ILogger<DatabaseConnectionFactory>? logger = null)
    {
        _logger = logger;
    }

    public IDbConnection CreateConnection(DatabaseConfig config)
    {
        var connectionString = BuildConnectionString(config);
        
        return config.Provider switch
        {
            DatabaseProvider.PostgreSQL => new NpgsqlConnection(connectionString),
         //   DatabaseProvider.SqlServer => new SqlConnection(connectionString),
            DatabaseProvider.MySQL => new MySqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database provider {config.Provider} is not supported")
        };
    }

    private string BuildConnectionString(DatabaseConfig config)
    {
        var baseConnectionString = config.ConnectionString;
        
        // For PostgreSQL, append optimized pool settings if not already present
        if (config.Provider == DatabaseProvider.PostgreSQL)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString);
            
            // Apply pool configuration
            connectionStringBuilder.Pooling = config.EnablePooling;
            connectionStringBuilder.MaxPoolSize = config.MaxPoolSize;
            connectionStringBuilder.MinPoolSize = config.MinPoolSize;
            connectionStringBuilder.ConnectionIdleLifetime = config.ConnectionIdleLifetimeMinutes * 60; // Convert to seconds
            connectionStringBuilder.CommandTimeout = config.CommandTimeoutSeconds;
            connectionStringBuilder.Timeout = config.ConnectionTimeoutSeconds;
            
            return connectionStringBuilder.ToString();
        }
        
        // For other providers, return the base connection string for now
        // TODO: Implement connection string building for SqlServer and MySQL
        return baseConnectionString;
    }

    public IOperationScope CreateOperationScope(DatabaseConfig config)
    {
        // Create a simple logger if none provided
        ILogger<OperationScope> logger = _logger != null 
            ? (ILogger<OperationScope>)Microsoft.Extensions.Logging.Abstractions.NullLogger<OperationScope>.Instance
            : Microsoft.Extensions.Logging.Abstractions.NullLogger<OperationScope>.Instance;
        
        return new OperationScope(this, config, logger);
    }
}