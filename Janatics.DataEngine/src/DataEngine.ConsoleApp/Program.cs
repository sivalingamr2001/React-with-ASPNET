using Janatics.DataEngine;
using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Infrastructure.Persistence;
using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Core.Auditing;
using Janatics.DataEngine.ProcessService.Core.Mapping;
using Janatics.DataEngine.ProcessService.Core.Processing;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Infrastructure.Resilience;
using Janatics.DataEngine.ProcessService.Models.RequestModels;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace DataEngine.ConsoleApp;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static async Task Main(string[] args)
    {
        var mode = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "both";
        var databaseConfig = BuildDatabaseConfig();
        var loggerFactory = NullLoggerFactory.Instance;

        WriteBanner(databaseConfig, mode);

        if (mode is "fetch" or "both")
        {
            await RunFetchSampleAsync(databaseConfig, loggerFactory).ConfigureAwait(false);
        }

        if (mode is "process" or "both")
        {
            await RunProcessSampleAsync(databaseConfig, loggerFactory).ConfigureAwait(false);
        }

        WriteSection("Complete");
        Console.WriteLine("Console samples finished.");
    }

    private static DatabaseConfig BuildDatabaseConfig()
    {
        return new DatabaseConfig
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = "Data Source=DataEngineDev.db;Cache=Shared",
            EnablePooling = true,
            MaxPoolSize = 50,
            MinPoolSize = 0,
            ConnectionTimeoutSeconds = 15,
            CommandTimeoutSeconds = 30,
            ConnectionIdleLifetimeMinutes = 5
        };
    }

    private static async Task RunFetchSampleAsync(DatabaseConfig databaseConfig, ILoggerFactory loggerFactory)
    {
        WriteSection("Fetch Service Sample");

        var resilientFactory = new ResilientConnectionFactory(
            loggerFactory.CreateLogger<ResilientConnectionFactory>(),
            loggerFactory,
            databaseConfig);

        var fetchOptions = new FetchServiceOptions
        {
            DatabaseConfig = databaseConfig,
            AllowDirectQueryExecution = true,
            DefaultPageSize = 10,
            MaxPageSize = 100
        };

        IQueryDefinitionRepository queryRepository = new QueryDefinitionRepository(fetchOptions, resilientFactory);
        ISqlValidator sqlValidator = new Janatics.DataEngine.FetchService.Core.SqlValidator();
        IFetchService fetchService = new Janatics.DataEngine.FetchService.Core.FetchService(
            fetchOptions,
            resilientFactory,
            queryRepository,
            sqlValidator,
            new NoOpMasterTablePreflight(),
            new NoOpActionFileLogger(),
            NullLogger<Janatics.DataEngine.FetchService.Core.FetchService>.Instance,
            null,
            null,
            null);

        var queryNumber = 1;
        var saveRequest = new SaveQueryDefinitionRequest
        {
            QueryNumber = queryNumber,
            Description = "Console sample fetch query",
            QueryText = "SELECT current_database() AS database_name, now() AS server_time, {sampleText} AS sample_text",
            Tables = Array.Empty<string>(),
            ParameterDefinitionJson = """{"sampleText":{"type":"text","required":true}}""",
            CreatedBy = "console-app",
            ModifiedBy = "console-app",
            IsActive = true
        };

        try
        {
            await EnsureFetchSchemaAsync(databaseConfig).ConfigureAwait(false);

            var savedQuery = await fetchService.SaveQueryAsync(saveRequest).ConfigureAwait(false);
            Console.WriteLine($"Saved fetch query #{savedQuery.QueryNumber}");
            Console.WriteLine(JsonSerializer.Serialize(savedQuery, JsonOptions));

            var executionResult = await fetchService.ExecuteAsync(new FetchExecutionRequest
            {
                QueryNumber = savedQuery.QueryNumber,
                Parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sampleText"] = "hello-from-console"
                },
                PageNumber = 1,
                PageSize = 5,
                ExecutedBy = "console-app"
            }).ConfigureAwait(false);

            Console.WriteLine("Fetch execution result:");
            Console.WriteLine(JsonSerializer.Serialize(executionResult, JsonOptions));

            var directQueryResult = await fetchService.ExecuteAsync(new FetchExecutionRequest
            {
                QueryNumber = 0,
                QueryText = "SELECT version() AS postgres_version, {requestedBy} AS requested_by",
                EnableDirectQueryExecution = true,
                Parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["requestedBy"] = "console-direct-query"
                },
                PageNumber = 1,
                PageSize = 1,
                ExecutedBy = "console-app"
            }).ConfigureAwait(false);

            Console.WriteLine("Direct fetch execution result:");
            Console.WriteLine(JsonSerializer.Serialize(directQueryResult, JsonOptions));

            var tables = await fetchService.GetTablesAsync().ConfigureAwait(false);
            Console.WriteLine($"Fetched metadata for {tables.Count} tables.");
            Console.WriteLine(JsonSerializer.Serialize(tables.Take(3), JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fetch sample failed.");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Make sure the fetch master tables script is applied before running this sample.");
        }
    }

    private static async Task RunProcessSampleAsync(DatabaseConfig databaseConfig, ILoggerFactory loggerFactory)
    {
        WriteSection("Process Service Sample");

        var resilientFactory = new ConfiguredResilientConnectionFactory(
            new ResilientConnectionFactory(
                loggerFactory.CreateLogger<ResilientConnectionFactory>(),
                loggerFactory,
                databaseConfig),
            databaseConfig);

        var fieldMapperLogger = loggerFactory.CreateLogger<FieldMapperService>();
        var dataTypeLogger = loggerFactory.CreateLogger<DataTypeConverter>();
        var processLogger = loggerFactory.CreateLogger<CoreProcessService>();
        IConfiguration configuration = new EmptyConfiguration();

        var fieldMapperService = new FieldMapperService(new ConsoleStubDataProvider(), fieldMapperLogger);
        var dataTypeConverter = new DataTypeConverter(dataTypeLogger);
        var processService = new CoreProcessService(
            processLogger,
            resilientFactory,
            databaseConfig,
            fieldMapperService,
            dataTypeConverter,
            new DataEngineOptions(),
            new DeterministicIdGenerator(),
            configuration,
            validationService: null,
            auditService: null,
            auditOutboxService: null);

        var processRequest = new ProcessRequest
        {
            RootEntityName = "console_sample_entity",
            RootEntityId = string.Empty,
            UserId = "console-app",
            UseModelBinding = true,
            EntityProperties = new Dictionary<string, object>
            {
                ["name"] = "Console Sample",
                ["status"] = "Draft",
                ["createdon"] = DateTime.UtcNow
            },
            NodeProps = new Dictionary<string, List<Dictionary<string, object>>>
            {
                ["console_child_entity"] = new()
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = "Child Row 1",
                        ["quantity"] = 2
                    }
                }
            }
        };

        try
        {
            var result = await processService.ProcessTransactionAsync(processRequest).ConfigureAwait(false);
            Console.WriteLine("Process execution result:");
            Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            Console.WriteLine("This sample uses model binding mode, so it exercises the process service call flow without requiring field mapper setup.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Process sample failed.");
            Console.WriteLine(ex.Message);
            Console.WriteLine("If the database is unavailable, the transaction scope in the resilient connection factory will fail before the sample can complete.");
        }
    }

    private static void WriteBanner(DatabaseConfig config, string mode)
    {
        Console.WriteLine("Janatics DataEngine Console Samples");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Provider: {config.Provider}");
        Console.WriteLine();
    }

    private static void WriteSection(string title)
    {
        Console.WriteLine($"=== {title} ===");
    }

    private static async Task EnsureFetchSchemaAsync(DatabaseConfig databaseConfig)
    {
        var scriptPath = databaseConfig.Provider switch
        {
            DatabaseProvider.PostgreSQL => Path.Combine(AppContext.BaseDirectory, "fetch-process-master-tables.postgresql.sql"),
            DatabaseProvider.Sqlite => Path.Combine(AppContext.BaseDirectory, "fetch-process-master-tables.sqlite.sql"),
            _ => null,
        };

        if (scriptPath is null)
        {
            Console.WriteLine("Fetch bootstrap schema is not supported for this database provider.");
            return;
        }

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Bootstrap SQL script was not found in the console output.", scriptPath);

        var script = await File.ReadAllTextAsync(scriptPath).ConfigureAwait(false);

        switch (databaseConfig.Provider)
        {
            case DatabaseProvider.PostgreSQL:
            {
                await using var pgConnection = new NpgsqlConnection(databaseConfig.ConnectionString);
                await pgConnection.OpenAsync().ConfigureAwait(false);
                await using var pgCommand = new NpgsqlCommand(script, pgConnection);
                await pgCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                break;
            }

            case DatabaseProvider.Sqlite:
            {
                await using var sqliteConnection = new SqliteConnection(databaseConfig.ConnectionString);
                await sqliteConnection.OpenAsync().ConfigureAwait(false);
                await using var sqliteCommand = new SqliteCommand(script, sqliteConnection);
                await sqliteCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                break;
            }
        }

        Console.WriteLine("Fetch bootstrap schema ensured.");
    }
}

internal sealed class ConsoleStubDataProvider : IDataProvider
{
    public Task<IDbConnection> GetConnectionAsync()
        => throw new NotSupportedException("ConsoleStubDataProvider does not open database connections.");

    public Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        => throw new NotSupportedException("ConsoleStubDataProvider does not support transactions.");

    public Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        => Task.FromResult(0);

    public Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction transaction)
        => Task.FromResult<object?>(null);

    public Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        => Task.FromResult(new DataTable());

    public Task<object?> ExecuteInsertWithReturnAsync(string tableName, Dictionary<string, object> parameters, string returnColumn, IDbTransaction transaction)
        => Task.FromResult<object?>(null);

    public Task<int> ExecuteUpdateAsync(string tableName, Dictionary<string, object> parameters, Dictionary<string, object> whereConditions, IDbTransaction transaction)
        => Task.FromResult(0);

    public Task<int> ExecuteDeleteAsync(string tableName, Dictionary<string, object> whereConditions, IDbTransaction transaction)
        => Task.FromResult(0);

    public string GetParameterPlaceholder(string parameterName)
        => $"@{parameterName}";

    public string FormatTableName(string tableName)
        => tableName;

    public Task<int> ExecuteNonQueryAsync(string sql, DbCommand command)
        => Task.FromResult(0);

    public Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction transaction)
        => Task.FromResult(0);
}

internal sealed class EmptyConfiguration : IConfiguration
{
    public string? this[string key]
    {
        get => null;
        set { }
    }

    public IEnumerable<IConfigurationSection> GetChildren()
        => Array.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken()
        => new EmptyChangeToken();

    public IConfigurationSection GetSection(string key)
        => new EmptyConfigurationSection(key);
}

internal sealed class EmptyConfigurationSection(string key) : IConfigurationSection
{
    public string? this[string key]
    {
        get => string.Empty;
        set { }
    }

    public string Key => key;
    public string Path => key;
    public string? Value { get; set; }

    public IEnumerable<IConfigurationSection> GetChildren()
        => Array.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken()
        => new EmptyChangeToken();

    public IConfigurationSection GetSection(string sectionKey)
        => new EmptyConfigurationSection(sectionKey);
}

internal sealed class EmptyChangeToken : Microsoft.Extensions.Primitives.IChangeToken
{
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        => EmptyDisposable.Instance;
}

internal sealed class EmptyDisposable : IDisposable
{
    public static EmptyDisposable Instance { get; } = new();
    public void Dispose() { }
}

internal sealed class ConfiguredResilientConnectionFactory(
    IResilientConnectionFactory inner,
    DatabaseConfig databaseConfig) : IResilientConnectionFactory
{
    private readonly IResilientConnectionFactory _inner = inner;
    private readonly DatabaseConfig _databaseConfig = databaseConfig;

    public IDbConnection CreateConnection(DatabaseConfig config) => _inner.CreateConnection(config);
    public IOperationScope CreateOperationScope(DatabaseConfig config) => _inner.CreateOperationScope(config);
    public Task<IDbConnection> CreateConnectionAsync(DatabaseConfig config) => _inner.CreateConnectionAsync(config);
    public Task<IOperationScope> CreateOperationScopeAsync(DatabaseConfig config) => _inner.CreateOperationScopeAsync(config);
    public Task<IOperationScope> CreateOperationScopeAsync() => _inner.CreateOperationScopeAsync(_databaseConfig);
    public Task<T> ExecuteInScopeAsync<T>(Func<IDbConnection, Task<T>> operation)
        => ExecuteWithConfiguredConnectionAsync(operation);

    public Task<T> ExecuteInTransactionScopeAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
        => ExecuteWithConfiguredTransactionAsync(operation);

    public Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        => transaction != null
            ? _inner.ExecuteNonQueryAsync(sql, parameters, transaction)
            : ExecuteWithConfiguredConnectionAsync(connection => _inner.ExecuteNonQueryAsync(sql, parameters, connection));

    public Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        => transaction != null
            ? _inner.ExecuteScalarAsync(sql, parameters, transaction)
            : ExecuteWithConfiguredConnectionAsync(connection => _inner.ExecuteScalarAsync(sql, parameters, connection));

    public Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbTransaction? transaction = null)
        => transaction != null
            ? _inner.ExecuteQueryAsync(sql, parameters, transaction)
            : ExecuteWithConfiguredConnectionAsync(connection => _inner.ExecuteQueryAsync(sql, parameters, connection));

    public Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbTransaction? transaction = null)
        => transaction != null
            ? _inner.BulkInsertAsync(tableName, dataTable, transaction)
            : throw new NotSupportedException("Bulk insert requires an explicit transaction.");

    public Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
        => _inner.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

    public Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
        => _inner.ExecuteScalarAsync(sql, parameters, connection, transaction);

    public Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, IDbConnection connection, IDbTransaction? transaction = null)
        => _inner.ExecuteQueryAsync(sql, parameters, connection, transaction);

    public Task<int> BulkInsertAsync(string tableName, DataTable dataTable, IDbConnection connection, IDbTransaction? transaction = null)
        => _inner.BulkInsertAsync(tableName, dataTable, connection, transaction);

    public ConnectionPoolMetrics GetPoolMetrics() => _inner.GetPoolMetrics();
    public Task<bool> ValidateConnectionHealthAsync(IDbConnection connection) => _inner.ValidateConnectionHealthAsync(connection);
    public Task<IDbConnection> CreateReadOnlyConnectionAsync(DatabaseConfig config, ReadReplicaConfig? readReplicaConfig = null)
        => _inner.CreateReadOnlyConnectionAsync(config, readReplicaConfig);
    public Task<ConnectionPoolAnalytics> GetPoolAnalyticsAsync() => _inner.GetPoolAnalyticsAsync();
    public Task RecordPerformanceMetricsAsync(ConnectionPerformanceMetrics metrics) => _inner.RecordPerformanceMetricsAsync(metrics);
    public Task<ConnectionPoolHealth> GetPoolHealthAsync() => _inner.GetPoolHealthAsync();

    private async Task<T> ExecuteWithConfiguredConnectionAsync<T>(Func<IDbConnection, Task<T>> operation)
    {
        using var connection = await _inner.CreateConnectionAsync(_databaseConfig).ConfigureAwait(false);
        return await operation(connection).ConfigureAwait(false);
    }

    private async Task<T> ExecuteWithConfiguredTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
    {
        using var connection = await _inner.CreateConnectionAsync(_databaseConfig).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await operation(connection, transaction).ConfigureAwait(false);
            transaction.Commit();
            return result;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Ignore rollback failure in sample wrapper.
            }

            throw;
        }
    }
}

internal sealed class NoOpMasterTablePreflight : IMasterTablePreflight
{
    public Task EnsureReadyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class NoOpActionFileLogger : IActionFileLogger
{
    public Task LogAsync(string serviceName, string actionName, string status, string details, string? performedBy = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
