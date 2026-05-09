// DataEngineWarmupService.cs
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Reflection;

namespace Janatics.DataEngine;

/// <summary>
/// Hosted service that auto-creates master tables by executing embedded SQL scripts before DataEngine is used.
/// Runs once at application startup and blocks until schema is ready.
/// </summary>
public sealed class DataEngineWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataEngineWarmupService> _logger;

    public DataEngineWarmupService(IServiceProvider serviceProvider, ILogger<DataEngineWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DataEngine warmup: checking master tables...");

        using var scope = _serviceProvider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DataEngineOptions>();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IResilientConnectionFactory>();
        var dbConfig = options.FetchService.DatabaseConfig;

        // Determine script resource name based on provider
        var scriptResourceName = dbConfig.Provider switch
        {
            DatabaseProvider.Sqlite => "fetch-process-master-tables.sqlite.sql",
            DatabaseProvider.PostgreSQL => "fetch-process-master-tables.postgresql.sql",
            DatabaseProvider.SqlServer => "fetch-process-master-tables.sqlserver.sql",
            DatabaseProvider.MySQL => "fetch-process-master-tables.mysql.sql",
            DatabaseProvider.Oracle => "fetch-process-master-tables.oracle.sql",
            _ => throw new NotSupportedException($"No master table script defined for provider '{dbConfig.Provider}'.")
        };

        // Load script from embedded resource (recommended) or file system fallback
        string sqlScript = await LoadScriptAsync(scriptResourceName, cancellationToken);

        if (string.IsNullOrWhiteSpace(sqlScript))
        {
            _logger.LogWarning("Master table script '{ScriptName}' not found or empty. Skipping auto-creation.", scriptResourceName);
            return;
        }

        // 1. Create connection
        using var connection = await connectionFactory.CreateConnectionAsync(dbConfig).ConfigureAwait(false);

        // 2. Begin Transaction (Synchronous - IDbConnection standard)
        using var transaction = connection.BeginTransaction();

        try
        {
            var statements = SplitScript(sqlScript, dbConfig.Provider);

            foreach (var statement in statements.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = statement.Trim();
                command.CommandTimeout = options.OperationTimeoutSeconds;

                // 3. Execute Synchronously (IDbCommand standard)
                command.ExecuteNonQuery();

                _logger.LogDebug("Executed SQL batch: {BatchPreview}",
                    statement.Length > 50 ? statement[..50] + "..." : statement);
            }

            // 4. Commit Synchronously
            transaction.Commit();
            _logger.LogInformation("DataEngine warmup: master tables created successfully.");
        }
        catch (Exception ex)
        {
            // 5. Rollback Synchronously
            transaction.Rollback();
            _logger.LogError(ex, "DataEngine warmup: failed to create master tables.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Load script from embedded resource first, then fallback to file system for dev workflows.
    /// </summary>
    private async Task<string> LoadScriptAsync(string scriptName, CancellationToken cancellationToken)
    {
        // 1. Try embedded resource (production/distributed)
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(scriptName, StringComparison.OrdinalIgnoreCase));

        if (resourcePath != null)
        {
            _logger.LogDebug("Loading master table script from embedded resource: {ResourcePath}", resourcePath);
            await using var stream = assembly.GetManifestResourceStream(resourcePath)!;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        // 2. Fallback to file system (development - your specific path)
        var devPaths = new[]
        {
            Path.Combine("FetchService", "Scripts", scriptName),
            Path.Combine("src", "Janatics.DataEngine", "FetchService", "Scripts", scriptName),
            @"D:\New Workspace\React-with-ASPNET\Janatics.DataEngine\src\Janatics.DataEngine\FetchService\Scripts\" + scriptName,
            Path.Combine(AppContext.BaseDirectory, "FetchService", "Scripts", scriptName)
        };

        foreach (var path in devPaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Loading master table script from file: {FilePath}", path);
                return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogWarning("Master table script '{ScriptName}' not found in embedded resources or paths: {Paths}",
            scriptName, string.Join(", ", devPaths));
        return string.Empty;
    }

    /// <summary>
    /// Split SQL script into executable batches based on provider dialect.
    /// </summary>
    private static IEnumerable<string> SplitScript(string script, DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => script.Split(new[] { "\nGO", "\nGO ", "\r\nGO", "\r\nGO " }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => s.Trim())
                                                .Where(s => !string.IsNullOrWhiteSpace(s)),
            _ => script.Split(';', StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .Where(s => !string.IsNullOrWhiteSpace(s))
        };
    }
}