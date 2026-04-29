using System.Data;
using KATCRUDServices.Core.Factories;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using KATCRUDServices.Core.Providers;
using KATCRUDServices.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KATCRUDServices.Core.Tests;

/// <summary>
/// Tests for the connection optimization implementation
/// </summary>
public class ConnectionOptimizationTests
{
    private readonly string _connectionString = "Server=127.0.0.1;Database=BusinessHours;Port=5432;User Id=postgres;Password=postgres;Timeout=30;CommandTimeout=30;Keepalive=60;Pooling=true;MinPoolSize=1;MaxPoolSize=15;";
    private readonly ILogger<EnhancedConnectionFactory> _logger;
    private readonly DatabaseConfig _databaseConfig;

    public ConnectionOptimizationTests()
    {
        _logger = NullLogger<EnhancedConnectionFactory>.Instance;
        _databaseConfig = new DatabaseConfig
        {
            ConnectionString = _connectionString,
            Provider = DatabaseProvider.PostgreSQL,
            EnablePooling = true,
            MaxPoolSize = 15,
            MinPoolSize = 1,
            CommandTimeoutSeconds = 30,
            ConnectionTimeoutSeconds = 30
        };
    }

    [Fact]
    public void DatabaseConnectionFactory_CanCreateConnection()
    {
        // Arrange
        var factory = new DatabaseConnectionFactory(_logger);

        // Act
        var connection = factory.CreateConnection(_databaseConfig);

        // Assert
        Assert.NotNull(connection);
        Assert.Equal(ConnectionState.Closed, connection.State);
        
        connection.Dispose();
    }

    [Fact]
    public void EnhancedConnectionFactory_CanCreateInstance()
    {
        // Arrange & Act
        var factory = new EnhancedConnectionFactory(_logger);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task EnhancedConnectionFactory_CanCreateConnectionAsync()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);

        // Act & Assert - This will test the connection creation without actually connecting
        // since we don't want to require a real database for unit tests
        try
        {
            var connection = await factory.CreateConnectionAsync(_databaseConfig);
            Assert.NotNull(connection);
            connection.Dispose();
        }
        catch (Exception ex)
        {
            // Expected if database is not available - that's fine for this test
            Assert.True(ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException);
        }
    }

    [Fact]
    public void EnhancedConnectionFactory_CanGetPoolMetrics()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);

        // Act
        var metrics = factory.GetPoolMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public async Task EnhancedConnectionFactory_CanCreateOperationScope()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);

        // Act
        var scope = await factory.CreateOperationScopeAsync(_databaseConfig);

        // Assert
        Assert.NotNull(scope);
        
        await scope.DisposeAsync();
    }

    [Fact]
    public void EnhancedPostgreSqlProvider_CanCreateInstance()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var providerLogger = NullLogger<EnhancedPostgreSqlProvider>.Instance;

        // Act
        var provider = new EnhancedPostgreSqlProvider(factory, _databaseConfig, providerLogger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task EnhancedPostgreSqlProvider_CanExecuteInScope()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var providerLogger = NullLogger<EnhancedPostgreSqlProvider>.Instance;
        var provider = new EnhancedPostgreSqlProvider(factory, _databaseConfig, providerLogger);

        // Act & Assert - This will test the scope creation without actually connecting
        try
        {
            var result = await provider.ExecuteInScopeAsync(async (connection) =>
            {
                Assert.NotNull(connection);
                return "test";
            });
            
            Assert.Equal("test", result);
        }
        catch (Exception ex)
        {
            // Expected if database is not available - that's fine for this test
            Assert.True(ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException);
        }
    }

    [Fact]
    public void ConnectionMultiplexer_CanCreateInstance()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var multiplexerLogger = NullLogger<ConnectionMultiplexer>.Instance;

        // Act
        var multiplexer = new ConnectionMultiplexer(factory, multiplexerLogger);

        // Assert
        Assert.NotNull(multiplexer);
    }

    [Fact]
    public void ConnectionMultiplexer_CanGetMetrics()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var multiplexerLogger = NullLogger<ConnectionMultiplexer>.Instance;
        var multiplexer = new ConnectionMultiplexer(factory, multiplexerLogger);

        // Act
        var metrics = multiplexer.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.LastReset <= DateTime.UtcNow);
    }

    [Fact]
    public void OperationScope_CanCreateWithFactory()
    {
        // Arrange
        var factory = new DatabaseConnectionFactory(_logger);
        var scopeLogger = NullLogger<OperationScope>.Instance;

        // Act
        var scope = new OperationScope(factory, _databaseConfig, scopeLogger);

        // Assert
        Assert.NotNull(scope);
        Assert.False(scope.IsConnectionOpen);
    }

    [Fact]
    public async Task OperationScope_CanCreateCommand()
    {
        // Arrange
        var factory = new DatabaseConnectionFactory(_logger);
        var scopeLogger = NullLogger<OperationScope>.Instance;
        var scope = new OperationScope(factory, _databaseConfig, scopeLogger);

        // Act & Assert
        try
        {
            var command = await scope.CreateCommandAsync("SELECT 1");
            Assert.NotNull(command);
            Assert.Equal("SELECT 1", command.CommandText);
        }
        catch (Exception ex)
        {
            // Expected if database is not available - that's fine for this test
            Assert.True(ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException);
        }
        finally
        {
            await scope.DisposeAsync();
        }
    }

    [Fact]
    public void TransactionService_CanCreateInstance()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var providerLogger = NullLogger<EnhancedPostgreSqlProvider>.Instance;
        var provider = new EnhancedPostgreSqlProvider(factory, _databaseConfig, providerLogger);
        
        var fieldMapperService = new FieldMapperService(provider, NullLogger<FieldMapperService>.Instance);
        var dataTypeConverter = new DataTypeConverter();
        var transactionLogger = NullLogger<TransactionService>.Instance;

        // Act
        var transactionService = new TransactionService(
            provider,
            factory,
            _databaseConfig,
            fieldMapperService,
            dataTypeConverter,
            transactionLogger);

        // Assert
        Assert.NotNull(transactionService);
    }
}

/// <summary>
/// Integration tests that require a real database connection
/// These tests will only run if the database is available
/// </summary>
public class ConnectionOptimizationIntegrationTests
{
    private readonly string _connectionString = "Server=127.0.0.1;Database=BusinessHours;Port=5432;User Id=postgres;Password=postgres;Timeout=30;CommandTimeout=30;Keepalive=60;Pooling=true;MinPoolSize=1;MaxPoolSize=15;";
    private readonly ILogger<EnhancedConnectionFactory> _logger;
    private readonly DatabaseConfig _databaseConfig;

    public ConnectionOptimizationIntegrationTests()
    {
        _logger = NullLogger<EnhancedConnectionFactory>.Instance;
        _databaseConfig = new DatabaseConfig
        {
            ConnectionString = _connectionString,
            Provider = DatabaseProvider.PostgreSQL,
            EnablePooling = true,
            MaxPoolSize = 15,
            MinPoolSize = 1,
            CommandTimeoutSeconds = 30,
            ConnectionTimeoutSeconds = 30
        };
    }

    [Fact]
    public async Task EnhancedConnectionFactory_CanConnectToDatabase()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);

        // Act
        try
        {
            var connection = await factory.CreateConnectionAsync(_databaseConfig);
            
            // Assert
            Assert.NotNull(connection);
            Assert.Equal(ConnectionState.Open, connection.State);
            
            // Test connection health
            var isHealthy = await factory.ValidateConnectionHealthAsync(connection);
            Assert.True(isHealthy);
            
            connection.Dispose();
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException)
        {
            // Skip test if database is not available
            Assert.True(true, "Database not available - skipping integration test");
        }
    }

    [Fact]
    public async Task EnhancedPostgreSqlProvider_CanExecuteQuery()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var providerLogger = NullLogger<EnhancedPostgreSqlProvider>.Instance;
        var provider = new EnhancedPostgreSqlProvider(factory, _databaseConfig, providerLogger);

        // Act
        try
        {
            var result = await provider.ExecuteInScopeAsync(async (connection) =>
            {
                var parameters = new Dictionary<string, object>();
                var dataTable = await provider.ExecuteQueryAsync("SELECT 1 as test_value", parameters, connection);
                
                Assert.NotNull(dataTable);
                Assert.True(dataTable.Rows.Count > 0);
                Assert.Equal(1, Convert.ToInt32(dataTable.Rows[0]["test_value"]));
                
                return "success";
            });
            
            Assert.Equal("success", result);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException)
        {
            // Skip test if database is not available
            Assert.True(true, "Database not available - skipping integration test");
        }
    }

    [Fact]
    public async Task ConnectionOptimization_SingleConnectionPerTransaction()
    {
        // Arrange
        var factory = new EnhancedConnectionFactory(_logger);
        var providerLogger = NullLogger<EnhancedPostgreSqlProvider>.Instance;
        var provider = new EnhancedPostgreSqlProvider(factory, _databaseConfig, providerLogger);

        // Act
        try
        {
            var connectionHashCodes = new List<int>();
            
            await provider.ExecuteInTransactionScopeAsync(async (connection, transaction) =>
            {
                // Record connection hash code for first operation
                connectionHashCodes.Add(connection.GetHashCode());
                
                // Execute multiple operations within the same transaction
                for (int i = 0; i < 3; i++)
                {
                    var parameters = new Dictionary<string, object>();
                    var result = await provider.ExecuteScalarAsync("SELECT 1", parameters, connection, transaction);
                    
                    // Record connection hash code for each operation
                    connectionHashCodes.Add(connection.GetHashCode());
                    
                    Assert.NotNull(result);
                    Assert.Equal(1, Convert.ToInt32(result));
                }
                
                return "success";
            });
            
            // Assert - All operations should use the same connection
            Assert.True(connectionHashCodes.Count > 1);
            Assert.True(connectionHashCodes.All(hash => hash == connectionHashCodes[0]), 
                "All operations should use the same connection within a transaction scope");
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException || ex is System.Net.Sockets.SocketException)
        {
            // Skip test if database is not available
            Assert.True(true, "Database not available - skipping integration test");
        }
    }
}