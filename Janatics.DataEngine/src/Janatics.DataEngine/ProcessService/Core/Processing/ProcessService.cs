using Janatics.DataEngine.Abstractions;
using Janatics.DataEngine.Core.Mapping;
using Janatics.DataEngine.FieldMapper.Model;
using Janatics.DataEngine.Infrastructure.Models;
using Janatics.DataEngine.Models.Audit;
using Janatics.DataEngine.Models.Execution;
using Janatics.DataEngine.Models.Metadata;
using Janatics.DataEngine.Models.RequestModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Janatics.DataEngine.Core.Processing;

public sealed class TriggerEntry
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object?> Payload { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;
    private readonly IResilientConnectionFactory _transactionDataProvider;
    private readonly DatabaseConfig _databaseConfig;
    private readonly FieldMapperService _fieldMapperService;
    private readonly DataTypeConverter _dataTypeConverter;
    private readonly IValidationService? _validationService;
    private readonly IAuditService? _auditService;
    private readonly IAuditOutboxService? _auditOutboxService;
    private readonly IConfiguration _configuration;
    private readonly DataEngineOptions _options;
    private readonly IDeterministicIdGenerator _idGenerator;

    public ProcessService(
        ILogger<ProcessService> logger,
        IResilientConnectionFactory transactionDataProvider,
        DatabaseConfig databaseConfig,
        FieldMapperService fieldMapperService,
        DataTypeConverter dataTypeConverter,
        DataEngineOptions options,
        IDeterministicIdGenerator idGenerator,
        IConfiguration? configuration = null,
        IValidationService? validationService = null,
        IAuditService? auditService = null,
        IAuditOutboxService? auditOutboxService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transactionDataProvider = transactionDataProvider ?? throw new ArgumentNullException(nameof(transactionDataProvider));
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _fieldMapperService = fieldMapperService ?? throw new ArgumentNullException(nameof(fieldMapperService));
        _dataTypeConverter = dataTypeConverter ?? throw new ArgumentNullException(nameof(dataTypeConverter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _configuration = configuration ?? new ConfigurationBuilder().Build();
        _validationService = validationService;
        _auditService = auditService;
        _auditOutboxService = auditOutboxService;
    }

    public async Task<ProcessResult> ProcessTransactionAsync(ProcessRequest request)
    {
        ValidateRequestBounds(request);

        var rootEntityId = string.IsNullOrEmpty(request.RootEntityId)
            ? _idGenerator.CreateId().ToString()
            : request.RootEntityId;
        var executionContext = new ProcessExecutionContext
        {
            RootEntityId = rootEntityId,
            RootEntityName = request.RootEntityName,
            State = ProcessExecutionState.Received,
            Depth = 0
        };

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Starting transaction {RootEntityId} for entity {EntityName}", rootEntityId, request.RootEntityName);

        List<AuditJob>? auditJobsToFlush = null;

        var result = await _transactionDataProvider.ExecuteInTransactionScopeAsync(async (connection, transaction) =>
        {
            try
            {
                auditJobsToFlush = _auditService?.PrepareAuditCacheForRequest();
                executionContext.State = ProcessExecutionState.Validated;

                ValidationResult? validationResult = null;
                if (_validationService != null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Validating transaction data for entity {EntityName}", request.RootEntityName);

                    validationResult = await _validationService.ValidateAsync(request.RootEntityName, request.EntityProperties, transaction).ConfigureAwait(false);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning("Validation failed for transaction {RootEntityId}: {Errors}", rootEntityId, errorMessages);

                        return new ProcessResult
                        {
                            Success = false,
                            RootEntityId = rootEntityId,
                            Message = $"Validation failed: {errorMessages}",
                            Data = new Dictionary<string, object>
                            {
                                { "ValidationErrors", validationResult.Errors.Select(e => new
                                    {
                                        e.FieldName,
                                        e.DisplayName,
                                        e.ErrorMessage,
                                        e.Rule
                                    }).ToList() }
                            }
                        };
                    }
                }

                var auditEnabledCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                bool isAuditEnabled = await IsAuditEnabledCachedAsync(request.RootEntityName, connection, transaction, auditEnabledCache).ConfigureAwait(false);

                var triggersCache = new Dictionary<string, List<TriggerEntry>>(StringComparer.OrdinalIgnoreCase);
                var mappersCache = new Dictionary<string, List<FieldMapperModel>>(StringComparer.OrdinalIgnoreCase);

                object renGuid;
                string operationType;
                Dictionary<string, object?>? previousData = null;

                if (request.UseModelBinding)
                {
                    executionContext.State = ProcessExecutionState.ProcessingRoot;
                    operationType = DetermineOperationTypeModelBinding(request);
                    var resultTuple = await CreateMainRecordModelBindingAsync(request, connection, transaction, rootEntityId, isAuditEnabled).ConfigureAwait(false);
                    renGuid = resultTuple.RenGuid;
                    previousData = resultTuple.PreviousData;
                    await ProcessChildRecordsModelBindingAsync(request, renGuid, connection, transaction, rootEntityId, triggersCache, isAuditEnabled, auditEnabledCache).ConfigureAwait(false);
                }
                else
                {
                    executionContext.State = ProcessExecutionState.ProcessingRoot;
                    var mainTableMappers = await GetFieldMappersCachedAsync(request.RootEntityName, transaction, mappersCache).ConfigureAwait(false);
                    if (!mainTableMappers.Any())
                    {
                        throw new InvalidOperationException($"No field mappers found for entity: {request.RootEntityName}");
                    }

                    operationType = DetermineOperationType(request, mainTableMappers);
                    var resultTuple = await CreateMainRecordAsync(request, mainTableMappers, transaction, rootEntityId, isAuditEnabled).ConfigureAwait(false);
                    renGuid = resultTuple.RenGuid;
                    previousData = resultTuple.PreviousData;
                    await ProcessChildRecordsAsync(request, renGuid, transaction, rootEntityId, triggersCache, isAuditEnabled, auditEnabledCache, mappersCache).ConfigureAwait(false);
                }

                await ProcessDeleteRecordsAsync(request, connection, transaction, rootEntityId).ConfigureAwait(false);
                if (auditJobsToFlush is { Count: > 0 } && _auditOutboxService != null)
                {
                    await _auditOutboxService.StageAsync(auditJobsToFlush, connection, transaction).ConfigureAwait(false);
                    auditJobsToFlush = null;
                }
                executionContext.State = ProcessExecutionState.Completed;
                _ = TriggerBackgroundProcessingAsync(request, renGuid, operationType, rootEntityId, triggersCache, previousData);

                return new ProcessResult
                {
                    Success = true,
                    RootEntityId = rootEntityId,
                    Message = "Transaction completed successfully",
                    Data = new Dictionary<string, object> { { "RENGUID", renGuid } }
                };
            }
            catch (Exception ex)
            {
                executionContext.State = ProcessExecutionState.Failed;
                _logger.LogError(ex, "Transaction {RootEntityId} failed", rootEntityId);
                throw;
            }
        }).ConfigureAwait(false);

        if (auditJobsToFlush is { Count: > 0 })
            _auditService?.FlushAuditLogsFireAndForget(_databaseConfig.ConnectionString, auditJobsToFlush);
        return result;
    }

    private void ValidateRequestBounds(ProcessRequest request)
    {
        var childCount = request.NodeProps.Values.Sum(list => list.Count);
        if (childCount > _options.MaxChildNodesPerRequest)
            throw new InvalidOperationException($"Child node count {childCount} exceeds configured maximum {_options.MaxChildNodesPerRequest}.");

        if (_options.MaxProcessDepth <= 0)
            throw new InvalidOperationException("MaxProcessDepth must be greater than zero.");
    }

    private Task<bool> IsAuditEnabledCachedAsync(string entityName, IDbConnection connection, IDbTransaction transaction, Dictionary<string, bool> cache)
    {
        if (cache.TryGetValue(entityName, out var cached))
        {
            return Task.FromResult(cached);
        }

        cache[entityName] = false;
        return Task.FromResult(false);
    }

    private async Task<List<FieldMapperModel>> GetFieldMappersCachedAsync(string entityName, IDbTransaction transaction, Dictionary<string, List<FieldMapperModel>> cache)
    {
        if (cache.TryGetValue(entityName, out var cached))
        {
            return cached;
        }

        var mappers = await _fieldMapperService.GetFieldMappersAsync(entityName, transaction).ConfigureAwait(false);
        cache[entityName] = mappers;
        return mappers;
    }

    private string DetermineOperationTypeModelBinding(ProcessRequest request)
    {
        return string.IsNullOrEmpty(request.RootEntityId) ? "CREATE" : "UPDATE";
    }

    private string DetermineOperationType(ProcessRequest request, List<FieldMapperModel> mappers)
    {
        return DetermineOperationTypeModelBinding(request);
    }

    private Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> CreateMainRecordModelBindingAsync(ProcessRequest request, IDbConnection connection, IDbTransaction transaction, string rootEntityId, bool isAuditEnabled)
    {
        Dictionary<string, object?>? previousData = new Dictionary<string, object?>();
        foreach (var kvp in request.EntityProperties)
        {
            previousData[kvp.Key] = kvp.Value;
        }

        return Task.FromResult<(object RenGuid, Dictionary<string, object?>? PreviousData)>((RenGuid: Guid.NewGuid().ToString(), PreviousData: previousData));
    }

    private Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> CreateMainRecordAsync(ProcessRequest request, List<FieldMapperModel> fieldMappers, IDbTransaction transaction, string rootEntityId, bool isAuditEnabled)
    {
        Dictionary<string, object?>? previousData = new Dictionary<string, object?>();
        foreach (var kvp in request.EntityProperties)
        {
            previousData[kvp.Key] = kvp.Value;
        }

        return Task.FromResult<(object RenGuid, Dictionary<string, object?>? PreviousData)>((RenGuid: Guid.NewGuid().ToString(), PreviousData: previousData));
    }

    private Task ProcessChildRecordsModelBindingAsync(ProcessRequest request, object renGuid, IDbConnection connection, IDbTransaction transaction, string rootEntityId, Dictionary<string, List<TriggerEntry>> triggersCache, bool isAuditEnabled, Dictionary<string, bool> auditEnabledCache)
    {
        return Task.CompletedTask;
    }

    private Task ProcessChildRecordsAsync(ProcessRequest request, object renGuid, IDbTransaction transaction, string rootEntityId, Dictionary<string, List<TriggerEntry>> triggersCache, bool isAuditEnabled, Dictionary<string, bool> auditEnabledCache, Dictionary<string, List<FieldMapperModel>> mappersCache)
    {
        return Task.CompletedTask;
    }

    private Task ProcessDeleteRecordsAsync(ProcessRequest request, IDbConnection connection, IDbTransaction transaction, string rootEntityId)
    {
        return Task.CompletedTask;
    }

    private Task TriggerBackgroundProcessingAsync(ProcessRequest request, object renGuid, string operationType, string rootEntityId, Dictionary<string, List<TriggerEntry>> triggersCache, Dictionary<string, object?>? previousData)
    {
        return Task.CompletedTask;
    }
}
