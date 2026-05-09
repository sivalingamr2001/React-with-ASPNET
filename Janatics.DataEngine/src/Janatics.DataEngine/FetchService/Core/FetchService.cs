using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.FetchService.Core;

public class FetchService : IFetchService
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<name>[a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    private readonly FetchServiceOptions _options;
    private readonly IResilientConnectionFactory _connectionFactory;
    private readonly IQueryDefinitionRepository _queryRepository;
    private readonly ISqlValidator _sqlValidator;
    private readonly IMasterTablePreflight _masterTablePreflight;
    private readonly IActionFileLogger _actionFileLogger;
    private readonly IQueryPlanCompiler _queryPlanCompiler;
    private readonly IQueryPlanCache _queryPlanCache;
    private readonly IMemoryCache _metadataCache;
    private readonly ILogger<FetchService> _logger;

    public FetchService(
        FetchServiceOptions options,
        IResilientConnectionFactory connectionFactory,
        IQueryDefinitionRepository queryRepository,
        ISqlValidator sqlValidator,
        IMasterTablePreflight masterTablePreflight,
        IActionFileLogger actionFileLogger,
        ILogger<FetchService> logger,
        IQueryPlanCompiler? queryPlanCompiler = null,
        IQueryPlanCache? queryPlanCache = null,
        IMemoryCache? metadataCache = null,
        bool _reserved = false)
    {
        _options = options;
        _connectionFactory = connectionFactory;
        _queryRepository = queryRepository;
        _sqlValidator = sqlValidator;
        _masterTablePreflight = masterTablePreflight;
        _actionFileLogger = actionFileLogger;
        _queryPlanCompiler = queryPlanCompiler ?? new DefaultQueryPlanCompiler();
        _queryPlanCache = queryPlanCache ?? new Infrastructure.Persistence.MemoryQueryPlanCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        _metadataCache = metadataCache ?? new MemoryCache(new MemoryCacheOptions());
        _logger = logger;
    }

    public async Task<QueryDefinitionModel> SaveQueryAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Description is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.QueryText) && string.IsNullOrWhiteSpace(request.FetchJson))
            throw new ArgumentException("Either QueryText or FetchJson must be provided.", nameof(request));

        if (!string.IsNullOrWhiteSpace(request.QueryText))
            _sqlValidator.ValidateReadOnlyQuery(request.QueryText);

        if (request.IsFetchJson && string.IsNullOrWhiteSpace(request.FetchJson))
            throw new ArgumentException("FetchJson must be provided when IsFetchJson is true.", nameof(request));

        var queryNumber = await _queryRepository.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        var actionName = request.QueryNumber.HasValue ? "UPDATE_QUERY" : "CREATE_QUERY";
        await _queryRepository.RecordAuditAsync(new FetchAuditLogEntry
        {
            QueryNumber = queryNumber,
            ActionName = actionName,
            ExecutedQuery = request.QueryText,
            ParametersJson = request.ParameterDefinitionJson,
            Status = "SUCCESS",
            CreatedBy = request.ModifiedBy ?? request.CreatedBy
        }, cancellationToken).ConfigureAwait(false);
        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            actionName,
            "SUCCESS",
            $"Query definition saved for querynumber={queryNumber}",
            request.ModifiedBy ?? request.CreatedBy,
            cancellationToken).ConfigureAwait(false);

        return await _queryRepository.GetAsync(queryNumber, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Saved query {queryNumber} could not be reloaded.");
    }

    public async Task<QueryDefinitionModel?> GetQueryAsync(long queryNumber, CancellationToken cancellationToken = default)
    {
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var model = await _queryRepository.GetAsync(queryNumber, cancellationToken).ConfigureAwait(false);
        if (model != null)
        {
            await _queryRepository.RecordAuditAsync(new FetchAuditLogEntry
            {
                QueryNumber = model.QueryNumber,
                ActionName = "GET_QUERY",
                Status = "SUCCESS",
                CreatedBy = null
            }, cancellationToken).ConfigureAwait(false);
        }

        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            "GET_QUERY",
            model == null ? "NOT_FOUND" : "SUCCESS",
            $"querynumber={queryNumber}",
            null,
            cancellationToken).ConfigureAwait(false);

        return model;
    }

    public async Task<IReadOnlyList<QueryDefinitionModel>> GetQueriesAsync(CancellationToken cancellationToken = default)
    {
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var items = await _queryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            "GET_QUERIES",
            "SUCCESS",
            $"count={items.Count}",
            null,
            cancellationToken).ConfigureAwait(false);
        return items;
    }

    public async Task<bool> DeleteQueryAsync(long queryNumber, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var deleted = await _queryRepository.DeleteAsync(queryNumber, deletedBy, cancellationToken).ConfigureAwait(false);
        await _queryRepository.RecordAuditAsync(new FetchAuditLogEntry
        {
            QueryNumber = queryNumber,
            ActionName = "DELETE_QUERY",
            Status = deleted ? "SUCCESS" : "NOT_FOUND",
            ErrorMessage = deleted ? null : "Query not found",
            CreatedBy = deletedBy
        }, cancellationToken).ConfigureAwait(false);
        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            "DELETE_QUERY",
            deleted ? "SUCCESS" : "NOT_FOUND",
            $"querynumber={queryNumber}",
            deletedBy,
            cancellationToken).ConfigureAwait(false);
        return deleted;
    }

    public Task<FetchExecutionResult> ExecuteQueryAsync(ExecuteStoredQueryRequest request, CancellationToken cancellationToken = default)
        => ExecuteAsync(request, cancellationToken);

    public async Task<FetchExecutionResult> ExecuteAsync(FetchExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        QueryDefinitionModel? queryDefinition = null;
        string sqlTemplate;
        Dictionary<string, object?> parameters;
        var resolvedPageNumber = Math.Max(1, request.PageNumber);
        var resolvedPageSize = Math.Clamp(request.PageSize, 1, _options.MaxPageSize);

        if (request.EnableDirectQueryExecution && !string.IsNullOrWhiteSpace(request.QueryText))
        {
            _sqlValidator.ValidateDirectQuery(request.QueryText, _options.AllowDirectQueryExecution, _options.MaxDirectQueryLength);
            sqlTemplate = BuildPaginatedSql(request.QueryText);
            (sqlTemplate, parameters) = BindParameters(sqlTemplate, request.Parameters, resolvedPageNumber, resolvedPageSize);
        }
        else
        {
            queryDefinition = await _queryRepository.GetAsync(request.QueryNumber, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Query number {request.QueryNumber} was not found.");

            if (queryDefinition.IsFetchJson)
            {
                var fetchJson = JsonSerializer.Deserialize<FetchJsonQuery>(queryDefinition.FetchJson ?? string.Empty)
                    ?? throw new InvalidOperationException("Invalid FetchJSON format.");

                fetchJson.Page = resolvedPageNumber;
                fetchJson.PageSize = resolvedPageSize;

                var generator = new FetchJsonSqlGenerator(_options.DatabaseConfig.Provider, _logger);
                var generated = generator.GenerateSql(fetchJson, request.Parameters);
                sqlTemplate = generated.Sql;
                parameters = generated.Parameters;
                _sqlValidator.ValidateReadOnlyQuery(sqlTemplate);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(queryDefinition.QueryText))
                    throw new InvalidOperationException($"Query number {request.QueryNumber} does not contain executable SQL.");

                _sqlValidator.ValidateReadOnlyQuery(queryDefinition.QueryText);
                sqlTemplate = BuildPaginatedSql(queryDefinition.QueryText);
                (sqlTemplate, parameters) = BindParameters(sqlTemplate, request.Parameters, resolvedPageNumber, resolvedPageSize);
            }
        }

        var cacheKey = BuildPlanCacheKey(
            request.QueryNumber,
            queryDefinition?.VersionNo ?? 0,
            sqlTemplate,
            _options.DatabaseConfig.Provider.ToString());

        if (!_queryPlanCache.TryGet(cacheKey, out var compiledPlan))
        {
            compiledPlan = _queryPlanCompiler.Compile(cacheKey, sqlTemplate, queryDefinition?.VersionNo ?? 0);
            _queryPlanCache.Set(compiledPlan, TimeSpan.FromMinutes(30));
        }

        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = compiledPlan!.SqlTemplate;

            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }

            using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
            var records = new List<Dictionary<string, object?>>();
            while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                    row[reader.GetName(index)] = await IsDBNullAsync(reader, index, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(index);
                records.Add(row);
            }

            await TryRecordAuditAsync(request.QueryNumber, new FetchAuditLogEntry
            {
                QueryNumber = request.QueryNumber,
                ActionName = "EXECUTE_QUERY",
                ExecutedQuery = compiledPlan!.SqlTemplate,
                ParametersJson = JsonSerializer.Serialize(parameters),
                ResultCount = records.Count,
                Status = "SUCCESS",
                CreatedBy = request.ExecutedBy
            }, cancellationToken).ConfigureAwait(false);
            await _actionFileLogger.LogAsync(
                nameof(FetchService),
                "EXECUTE_QUERY",
                "SUCCESS",
                $"querynumber={request.QueryNumber}, records={records.Count}",
                request.ExecutedBy,
                cancellationToken).ConfigureAwait(false);

            return new FetchExecutionResult
            {
                Success = true,
                Message = "Query executed successfully.",
                QueryNumber = request.QueryNumber,
                Records = records,
                RecordCount = records.Count,
                PageNumber = resolvedPageNumber,
                PageSize = resolvedPageSize
            };
        }
        catch (Exception ex)
        {
            await TryRecordAuditAsync(request.QueryNumber, new FetchAuditLogEntry
            {
                QueryNumber = request.QueryNumber,
                ActionName = "EXECUTE_QUERY",
                ExecutedQuery = compiledPlan!.SqlTemplate,
                ParametersJson = JsonSerializer.Serialize(parameters),
                Status = "FAILED",
                ErrorMessage = ex.Message,
                CreatedBy = request.ExecutedBy
            }, cancellationToken).ConfigureAwait(false);
            await _actionFileLogger.LogAsync(
                nameof(FetchService),
                "EXECUTE_QUERY",
                "FAILED",
                $"querynumber={request.QueryNumber}, error={ex.Message}",
                request.ExecutedBy,
                cancellationToken).ConfigureAwait(false);

            _logger.LogError(ex, "Error executing fetch query {QueryNumber}", request.QueryNumber);
            throw;
        }
    }

    public async Task<IReadOnlyList<TableMetadataModel>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        const string cacheKey = "fetch:metadata:tables:v1";
        if (_metadataCache.TryGetValue(cacheKey, out IReadOnlyList<TableMetadataModel>? cachedTables) && cachedTables is not null)
            return cachedTables;

        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);
        var sql = _options.DatabaseConfig.Provider switch
        {
            DatabaseProvider.Sqlite => """
                SELECT name
                FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite_%'
                ORDER BY name
                """,
            _ => """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = @schemaName
                  AND table_type = 'BASE TABLE'
                ORDER BY table_name
                """
        };

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (_options.DatabaseConfig.Provider != DatabaseProvider.Sqlite)
        {
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@schemaName";
            schemaParam.Value = _options.SchemaName;
            command.Parameters.Add(schemaParam);
        }

        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

        var tables = new List<TableMetadataModel>();
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
            tables.Add(new TableMetadataModel { Name = reader.GetString(0) });

        foreach (var table in tables)
            table.Columns = await GetTableColumnsAsync(table.Name, cancellationToken).ConfigureAwait(false);

        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            "GET_TABLES",
            "SUCCESS",
            $"count={tables.Count}",
            null,
            cancellationToken).ConfigureAwait(false);

        _metadataCache.Set(cacheKey, tables, TimeSpan.FromMinutes(5));
        return tables;
    }

    public async Task<IReadOnlyList<ColumnMetadataModel>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        await _masterTablePreflight.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name is required.", nameof(tableName));

        var cacheKey = $"fetch:metadata:columns:{tableName.ToLowerInvariant()}";
        if (_metadataCache.TryGetValue(cacheKey, out IReadOnlyList<ColumnMetadataModel>? cachedColumns) && cachedColumns is not null)
            return cachedColumns;

        using var connection = await _connectionFactory.CreateConnectionAsync(_options.DatabaseConfig).ConfigureAwait(false);

        var sql = _options.DatabaseConfig.Provider switch
        {
            DatabaseProvider.Sqlite => $"PRAGMA table_info('{tableName.Replace("'", "''")}')",
            _ => """
                SELECT
                    c.column_name,
                    c.data_type,
                    c.is_nullable,
                    c.ordinal_position,
                    CASE WHEN pk.column_name IS NULL THEN FALSE ELSE TRUE END AS is_primary_key
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON tc.constraint_name = kcu.constraint_name
                       AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                ) pk
                    ON pk.table_schema = c.table_schema
                   AND pk.table_name = c.table_name
                   AND pk.column_name = c.column_name
                WHERE c.table_schema = @schemaName
                  AND c.table_name = @tablename
                ORDER BY c.ordinal_position
                """
        };

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (_options.DatabaseConfig.Provider != DatabaseProvider.Sqlite)
        {
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@schemaName";
            schemaParam.Value = _options.SchemaName;
            command.Parameters.Add(schemaParam);

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tablename";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
        }

        using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

        var columns = new List<ColumnMetadataModel>();
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            columns.Add(new ColumnMetadataModel
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                OrdinalPosition = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IsPrimaryKey = !reader.IsDBNull(4) && reader.GetBoolean(4)
            });
        }

        await _actionFileLogger.LogAsync(
            nameof(FetchService),
            "GET_TABLE_COLUMNS",
            "SUCCESS",
            $"table={tableName}, count={columns.Count}",
            null,
            cancellationToken).ConfigureAwait(false);

        _metadataCache.Set(cacheKey, columns, TimeSpan.FromMinutes(5));
        return columns;
    }

    private string BuildPaginatedSql(string sql)
    {
        _sqlValidator.ValidateReadOnlyQuery(sql);
        return $"{sql.Trim()}{Environment.NewLine}LIMIT @__pageSize OFFSET @__offset";
    }

    private static (string Sql, Dictionary<string, object?> Parameters) BindParameters(string sql, IDictionary<string, object?> suppliedParameters, int pageNumber, int pageSize)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var rewritten = PlaceholderRegex.Replace(sql, match =>
        {
            var name = match.Groups["name"].Value;
            if (!suppliedParameters.TryGetValue(name, out var value))
                throw new InvalidOperationException($"Missing required query parameter '{name}'.");

            parameters[$"@{name}"] = value;
            return $"@{name}";
        });

        parameters["@__pageSize"] = pageSize;
        parameters["@__offset"] = (Math.Max(1, pageNumber) - 1) * pageSize;
        return (rewritten, parameters);
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        return command.ExecuteReader();
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbDataReader)
            return await dbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return reader.Read();
    }

    private static async Task<bool> IsDBNullAsync(IDataReader reader, int ordinal, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbDataReader)
            return await dbDataReader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false);

        return reader.IsDBNull(ordinal);
    }

    private async Task TryRecordAuditAsync(long queryNumber, FetchAuditLogEntry entry, CancellationToken cancellationToken)
    {
        if (queryNumber <= 0)
            return;

        await _queryRepository.RecordAuditAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildPlanCacheKey(long queryNumber, int versionNo, string sqlTemplate, string provider)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sqlTemplate));
        var hash = Convert.ToHexString(hashBytes);
        return $"{provider}:{queryNumber}:{versionNo}:{hash}";
    }
}
