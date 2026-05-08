using DataEngine.AuditService.Interface;
using DataEngine.AuditService.Models;
using DataEngine.FieldMapper.Interface;
using DataEngine.FieldMapper.Model;
using DataEngine.FieldMapper.Service;
using DataEngine.Infrastruture.Models;
using DataEngine.ProcessService.Interfaces;
using DataEngine.ProcessService.Model;
using DataEngine.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq;

namespace DataEngine.ProcessService.Services;

public sealed class TriggerEntry
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object?> Payload { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ProcessService : IProcess
{
    private readonly ILogger<ProcessService> _logger;
    private readonly IResilientDataProvider _transactionDataProvider;
    private readonly DatabaseConfig _databaseConfig;
    private readonly FieldMapperService _fieldMapperService;
    private readonly DataTypeConverter _dataTypeConverter;
    private readonly IValidationService? _validationService;
    private readonly IAuditService? _auditService;
    private readonly IConfiguration _configuration;

    public ProcessService(
        ILogger<ProcessService> logger,
        IResilientDataProvider transactionDataProvider,
        DatabaseConfig databaseConfig,
        FieldMapperService fieldMapperService,
        DataTypeConverter dataTypeConverter,
        IConfiguration configuration,
        IValidationService? validationService = null,
        IAuditService? auditService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transactionDataProvider = transactionDataProvider ?? throw new ArgumentNullException(nameof(transactionDataProvider));
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _fieldMapperService = fieldMapperService ?? throw new ArgumentNullException(nameof(fieldMapperService));
        _dataTypeConverter = dataTypeConverter ?? throw new ArgumentNullException(nameof(dataTypeConverter));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _validationService = validationService;
        _auditService = auditService;
    }

    public async Task<ProcessResult> ProcessTransactionAsync(ProcessRequest request)
    {
        var rootEntityId = string.IsNullOrEmpty(request.RootEntityId)
            ? Guid.NewGuid().ToString()
            : request.RootEntityId;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Starting transaction {RootEntityId} for entity {EntityName}", rootEntityId, request.RootEntityName);

        List<AuditJob>? auditJobsToFlush = null;

        var result = await _transactionDataProvider.ExecuteInTransactionScopeAsync(async (connection, transaction) =>
        {
            try
            {
                auditJobsToFlush = _auditService?.PrepareAuditCacheForRequest();

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
                    operationType = DetermineOperationTypeModelBinding(request);
                    var resultTuple = await CreateMainRecordModelBindingAsync(request, connection, transaction, rootEntityId, isAuditEnabled).ConfigureAwait(false);
                    renGuid = resultTuple.RenGuid;
                    previousData = resultTuple.PreviousData;
                    await ProcessChildRecordsModelBindingAsync(request, renGuid, connection, transaction, rootEntityId, triggersCache, isAuditEnabled, auditEnabledCache).ConfigureAwait(false);
                }
                else
                {
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
                _logger.LogError(ex, "Transaction {RootEntityId} failed", rootEntityId);
                throw;
            }
        }).ConfigureAwait(false);

        _auditService?.FlushAuditLogsFireAndForget(_databaseConfig.ConnectionString, auditJobsToFlush);
        return result;
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

        return Task.FromResult<(object RenGuid, Dictionary<string, object?>? PreviousData)>((RenGuid: (object)Guid.NewGuid().ToString(), PreviousData: previousData));
    }

    private Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> CreateMainRecordAsync(ProcessRequest request, List<FieldMapperModel> fieldMappers, IDbTransaction transaction, string rootEntityId, bool isAuditEnabled)
    {
        Dictionary<string, object?>? previousData = new Dictionary<string, object?>();
        foreach (var kvp in request.EntityProperties)
        {
            previousData[kvp.Key] = kvp.Value;
        }

        return Task.FromResult<(object RenGuid, Dictionary<string, object?>? PreviousData)>((RenGuid: (object)Guid.NewGuid().ToString(), PreviousData: previousData));
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
