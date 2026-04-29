using Google.Protobuf.WellKnownTypes;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using KATCRUDServices.Core.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;

namespace KATCRUDServices.Core.Services
{
    public partial class TransactionService : ITransactionService
    {
        private readonly IEnhancedDataProvider _transactionDataProvider;
        private readonly IEnhancedConnectionFactory _connectionFactory;
        private readonly DatabaseConfig _databaseConfig;
        private readonly FieldMapperService _fieldMapperService;
        private readonly DataTypeConverter _dataTypeConverter;
        private readonly IValidationService? _validationService;
        private readonly IAuditService? _auditService;
        private readonly ILogger<TransactionService> _logger;
        private readonly IConfiguration _configuration;

        public TransactionService(
            IEnhancedDataProvider transactionDataProvider,
            IEnhancedConnectionFactory connectionFactory,
            DatabaseConfig databaseConfig,
            FieldMapperService fieldMapperService,
            DataTypeConverter dataTypeConverter,
            ILogger<TransactionService> logger,
            IConfiguration configuration = null,
            IValidationService? validationService = null,
            IAuditService? auditService = null)
        {
            _transactionDataProvider = transactionDataProvider ?? throw new ArgumentNullException(nameof(transactionDataProvider));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
            _fieldMapperService = fieldMapperService;
            _dataTypeConverter = dataTypeConverter;
            _validationService = validationService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;

            // Set the data provider for DAG trigger repository
            DagTriggerRepository.SetDataProvider(transactionDataProvider);

            // Initialize DAG trigger service with configuration
            // Note: Token refresh happens lazily when TriggerDagAsync is called, not during initialization
            if (configuration != null)
            {
                try
                {
                    DagTriggerService.Initialize(configuration);
                }
                catch (Exception ex)
                {
                    // Log warning but don't fail service construction - configuration values are still set
                    _logger.LogWarning(ex, "Failed to initialize DagTriggerService configuration during TransactionService construction: {Message}", ex.Message);
                }
            }

            // Initialize background trigger service
            BackgroundTriggerService.Initialize(transactionDataProvider, this, logger);
        }

        public async Task<TransactionResult> ProcessTransactionAsync(TransactionRequest request)
        {
            var transactionId = string.IsNullOrEmpty(request.TransactionId)
                ? Guid.NewGuid().ToString()
                : request.TransactionId;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Starting transaction {TransactionId} for entity {EntityName}", transactionId, request.TransactionEntityName);

            // Audit jobs collected in the scope are passed to flush so they are not lost (AsyncLocal can be empty after await)
            List<AuditJob>? auditJobsToFlush = null;

            // Use enhanced connection scope for the entire transaction (target: complete within 1 second)
            var result = await _transactionDataProvider.ExecuteInTransactionScopeAsync(async (connection, transaction) =>
            {
                try
                {
                    // Prepare audit cache for this request so we can pass it to flush after commit
                    auditJobsToFlush = _auditService?.PrepareAuditCacheForRequest();

                    KATCRUDServices.Core.Models.ValidationResult? validationResult = null;
                    if (_validationService != null)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Validating transaction data for entity {EntityName}", request.TransactionEntityName);
                        validationResult = await _validationService.ValidateAsync(request.TransactionEntityName, request.ExtendedProperties, transaction).ConfigureAwait(false);

                        if (!validationResult.IsValid)
                        {
                            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                            _logger.LogWarning("Validation failed for transaction {TransactionId}: {Errors}", transactionId, errorMessages);

                            return new TransactionResult
                            {
                                Success = false,
                                TransactionId = transactionId,
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

                    // Audit and mappers (cached for this request)
                    var auditEnabledCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    bool isAuditEnabled = await IsAuditEnabledCachedAsync(request.TransactionEntityName, connection, transaction, auditEnabledCache).ConfigureAwait(false);

                    Dictionary<string, List<TriggerEntry>> _trggersCache = new();
                    var mappersCache = new Dictionary<string, List<FieldMapper>>(StringComparer.OrdinalIgnoreCase);

                    object renGuid;
                    string operationType;
                    Dictionary<string, object?>? previousData = null;

                    if (request.UseModelBinding)
                    {
                        operationType = DetermineOperationTypeModelBinding(request);

                        var resultTuple = await CreateMainRecordModelBindingAsync(request, connection, transaction, transactionId, isAuditEnabled).ConfigureAwait(false);
                        renGuid = resultTuple.RenGuid;
                        previousData = resultTuple.PreviousData;

                        await ProcessChildRecordsModelBindingAsync(request, renGuid, connection, transaction, transactionId, _trggersCache, isAuditEnabled, auditEnabledCache).ConfigureAwait(false);
                    }
                    else
                    {
                        var mainTableMappers = await GetFieldMappersCachedAsync(request.TransactionEntityName, transaction, mappersCache).ConfigureAwait(false);

                        if (!mainTableMappers.Any())
                        {
                            throw new InvalidOperationException($"No field mappers found for entity: {request.TransactionEntityName}");
                        }

                        operationType = DetermineOperationType(request, mainTableMappers);

                        var resultTuple = await CreateMainRecordAsync(request, mainTableMappers, transaction, transactionId, isAuditEnabled).ConfigureAwait(false);
                        renGuid = resultTuple.RenGuid;
                        previousData = resultTuple.PreviousData;

                        await ProcessChildRecordsAsync(request, renGuid, transaction, transactionId, _trggersCache, isAuditEnabled, auditEnabledCache, mappersCache).ConfigureAwait(false);
                    }

                    await ProcessDeleteRecordsAsync(request, connection, transaction, transactionId).ConfigureAwait(false);

                    // Step 8: Trigger background processing (fire-and-forget so commit is not delayed)
                    _ = TriggerBackgroundProcessingAsync(request, renGuid, operationType, transactionId, _trggersCache, previousData);

                    return new TransactionResult
                    {
                        Success = true,
                        TransactionId = transactionId,
                        Message = "Transaction completed successfully",
                        Data = new Dictionary<string, object> { { "RENGUID", renGuid } }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction {TransactionId} failed", transactionId);
                    throw;
                }
            }).ConfigureAwait(false);

            // After transaction commit: flush the audit jobs we collected in the scope (pass list explicitly so it is not lost)
            _auditService?.FlushAuditLogsFireAndForget(_databaseConfig.ConnectionString, auditJobsToFlush);

            return result;
        }

        // Placeholder methods - these would need to be implemented based on the original file
        private string DetermineOperationTypeModelBinding(TransactionRequest request)
        {
            if (HasValidRequestTransactionId(request))
            {
                return "Update";
            }

            // Check if 'id' property exists and has a value
            if (request.ExtendedProperties.TryGetValue("id", out var idValue))
            {
                if (idValue != null && !string.IsNullOrWhiteSpace(idValue.ToString()) &&
                    idValue.ToString() != "00000000-0000-0000-0000-000000000000" &&
                    idValue.ToString() != "0")
                {
                    return "Update";
                }
            }
            return "Insert";
        }

        private string DetermineOperationType(TransactionRequest request, List<FieldMapper> mappers)
        {
            if (HasValidRequestTransactionId(request))
            {
                return "Update";
            }

            // Check if an ID field is provided in the request
            var idMapper = mappers.FirstOrDefault(m => m.Properties.Contains("AutoGenerated"));

            if (idMapper != null && request.ExtendedProperties.TryGetValue(idMapper.FieldName, out var idValue))
            {
                if (idValue != null && !string.IsNullOrWhiteSpace(idValue.ToString()))
                {
                    return "Update";
                }
            }
            return "Insert";
        }

        private async Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> CreateMainRecordModelBindingAsync(
            TransactionRequest request,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            _logger.LogInformation("Creating main record using model binding for entity {EntityName}", request.TransactionEntityName);

            try
            {
                EnsureRequestIdFromTransactionId(request);

                var operationType = DetermineOperationTypeModelBinding(request);

                if (operationType == "Update")
                {
                    return await UpdateMainRecordModelBindingAsync(request, connection, transaction, transactionId, isAuditEnabled);
                }

                // approvalprocesses table uses processid (int) and createddate, not id/createdon
                if (string.Equals(request.TransactionEntityName, "approvalprocesses", StringComparison.OrdinalIgnoreCase))
                {
                    return await CreateApprovalProcessRecordAsync(request, connection, transaction, transactionId, isAuditEnabled);
                }

                // INSERT operation
                var tableName = request.TransactionEntityName;
                var properties = request.ExtendedProperties ?? new Dictionary<string, object>();
                var columns = new List<string>();
                var values = new List<string>();
                var parameters = new Dictionary<string, object>();
                var renGuid = Guid.NewGuid();

                // Add RENGUID as primary key
                columns.Add("id");
                values.Add("@id");
                parameters["id"] = renGuid;

                // Process all properties
                foreach (var prop in properties)
                {
                    if (prop.Key.ToLower() == "id") continue; // Skip ID for insert

                    var columnName = prop.Key;
                    var paramName = $"@{columnName}";

                    columns.Add(columnName);
                    values.Add(paramName);

                    // Handle special values
                    if (prop.Value?.ToString()?.ToLower() == "now")
                    {
                        parameters[columnName] = DateTime.UtcNow;
                    }
                    else if (prop.Value?.ToString() == "|RENGUID|")
                    {
                        parameters[columnName] = renGuid;
                    }
                    else
                    {
                        parameters[columnName] = prop.Value ?? DBNull.Value;
                    }
                }

                if (!columns.Any(c => c.Equals("createdon", StringComparison.OrdinalIgnoreCase)))
                {
                    columns.Add("createdon");
                    values.Add("@createdon");
                    parameters["createdon"] = DateTime.UtcNow;
                }

                if (properties.ContainsKey("userid"))
                {
                    columns.Add("createdby");
                    values.Add("@createdby");
                    parameters["createdby"] = properties["userid"];
                }

                var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

                _logger.LogDebug("Executing INSERT SQL: {Sql}", sql);

                await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

                _logger.LogInformation("Successfully created main record with ID {RenGuid}", renGuid);

                // AUDIT: Fire-and-forget audit for CREATE operation (only if enabled)
                if (_auditService != null && isAuditEnabled)
                {
                    var modifiedBy = properties.ContainsKey("userid")
                        ? properties["userid"]?.ToString() ?? "system"
                        : "system";

                    _ = _auditService.TryAuditAsync(
                        tableName,
                        renGuid,
                        AuditOperation.CREATE,
                        modifiedBy,
                        properties,
                        connection,
                        transaction);
                }

                return (renGuid, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create main record for entity {EntityName}", request.TransactionEntityName);
                throw;
            }
        }

        private async Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> CreateApprovalProcessRecordAsync(
            TransactionRequest request,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            var tableName = request.TransactionEntityName;
            var properties = request.ExtendedProperties ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var parameters = new Dictionary<string, object>();

            foreach (var prop in properties)
            {
                var key = prop.Key.ToLowerInvariant();
                if (key == "id" || key == "processid") continue;
                parameters[prop.Key] = prop.Value ?? DBNull.Value;
            }

            // Ensure description is always included (column in approvalprocesses table)
            var descValue = GetFirstValueExtended(properties, "description", "processdescription", "Description");
            parameters["description"] = descValue != null && !string.IsNullOrWhiteSpace(descValue.ToString()) ? descValue : string.Empty;

            if (!parameters.ContainsKey("createddate"))
            {
                parameters["createddate"] = DateTime.UtcNow;
            }

            var result = await _transactionDataProvider.ExecuteInsertWithReturnAsync(
                tableName, parameters, "processid", transaction);

            if (result != null)
            {
                _logger.LogInformation("Created approval process with processid {ProcessId}", result);
                if (_auditService != null && isAuditEnabled)
                {
                    var modifiedBy = properties.ContainsKey("userid")
                        ? properties["userid"]?.ToString() ?? "system"
                        : "system";
                    _ = _auditService.TryAuditAsync(
                        tableName,
                        Guid.Empty,
                        AuditOperation.CREATE,
                        modifiedBy,
                        properties,
                        connection,
                        transaction);
                }
            }

            return (result ?? 0, null);
        }

        private async Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> UpdateMainRecordModelBindingAsync(
            TransactionRequest request,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            _logger.LogInformation("Updating main record using model binding for entity {EntityName}", request.TransactionEntityName);

            try
            {
                var tableName = request.TransactionEntityName;
                var properties = request.ExtendedProperties ?? new Dictionary<string, object>();

                // approvalprocesses uses processid (int), not id (guid)
                if (string.Equals(tableName, "approvalprocesses", StringComparison.OrdinalIgnoreCase))
                {
                    return await UpdateApprovalProcessRecordAsync(request, connection, transaction, transactionId, isAuditEnabled);
                }

                if (!properties.TryGetValue("id", out var idValue) || idValue == null)
                {
                    throw new InvalidOperationException("ID is required for update operations");
                }

                var recordId = Guid.Parse(idValue.ToString()!);
                var modifiedBy = properties.ContainsKey("userid")
                    ? properties["userid"]?.ToString() ?? "system"
                    : "system";

                Dictionary<string, object?>? previousData = null;

                // AUDIT: Call BEFORE update to capture existing data (only if enabled, or always if we need previous data)
                if (_auditService != null)
                {
                    previousData = await _auditService.ReadExistingDataAsync(
                        tableName,
                        recordId,
                        connection,
                        transaction);

                    if (isAuditEnabled)
                    {
                        await _auditService.TryAuditAsync(
                            tableName,
                            recordId,
                            AuditOperation.UPDATE,
                            modifiedBy,
                            properties,
                            connection,
                            transaction,
                            previousData);
                    }
                }

                var setClauses = new List<string>();
                var parameters = new Dictionary<string, object>();
                parameters["id"] = recordId;

                // Process all properties except ID
                foreach (var prop in properties)
                {
                    if (prop.Key.ToLower() == "id") continue; // Skip ID for update

                    var columnName = prop.Key;
                    var paramName = $"@{columnName}";

                    setClauses.Add($"{columnName} = {paramName}");

                    // Handle special values
                    if (prop.Value?.ToString()?.ToLower() == "now")
                    {
                        parameters[columnName] = DateTime.UtcNow;
                    }
                    else if (prop.Value?.ToString() == "|RENGUID|")
                    {
                        parameters[columnName] = idValue; // Use the existing ID
                    }
                    else
                    {
                        parameters[columnName] = prop.Value ?? DBNull.Value;
                    }
                }

                // Add audit fields
                setClauses.Add("modifiedon = @modifiedon");
                parameters["modifiedon"] = DateTime.UtcNow;

                if (properties.ContainsKey("userid"))
                {
                    setClauses.Add("modifiedby = @modifiedby");
                    parameters["modifiedby"] = properties["userid"];
                }

                var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE id = @id";

                _logger.LogDebug("Executing UPDATE SQL: {Sql}", sql);

                var rowsAffected = await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"No record found with ID {idValue} to update");
                }

                _logger.LogInformation("Successfully updated main record with ID {Id}", idValue);
                return (idValue, previousData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update main record for entity {EntityName}", request.TransactionEntityName);
                throw;
            }
        }

        private async Task<(object RenGuid, Dictionary<string, object?>? PreviousData)> UpdateApprovalProcessRecordAsync(
            TransactionRequest request,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            var tableName = request.TransactionEntityName;
            var properties = request.ExtendedProperties ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var processIdValue = GetFirstValueExtended(properties, "processid", "id");
            if (processIdValue == null || string.IsNullOrWhiteSpace(processIdValue.ToString()))
            {
                throw new InvalidOperationException("processid (or id) is required for approval process update");
            }

            var processId = processIdValue.ToString()!.Trim();
            var setParams = new Dictionary<string, object>();

            foreach (var prop in properties)
            {
                var key = prop.Key.ToLowerInvariant();
                if (key == "id" || key == "processid") continue;
                setParams[prop.Key] = prop.Value ?? DBNull.Value;
            }

            // Ensure description is always in the UPDATE (approvalprocesses column)
            var descValue = GetFirstValueExtended(properties, "description", "processdescription", "Description");
            setParams["description"] = descValue != null && !string.IsNullOrWhiteSpace(descValue.ToString()) ? descValue : string.Empty;

            var whereConditions = new Dictionary<string, object>();
            whereConditions["processid"] = int.TryParse(processId, out var pid) ? pid : processIdValue;

            var rowsAffected = await _transactionDataProvider.ExecuteUpdateAsync(
                tableName, setParams, whereConditions, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"No approval process found with processid {processId} to update");
            }

            _logger.LogInformation("Updated approval process with processid {ProcessId}", processId);

            Dictionary<string, object?>? previousData = null;

            if (_auditService != null)
            {
                // Approval process tables typically use integer ID 'processid' or similarly don't use Guids, 
                // but ReadExistingDataAsync currently expects Guid. 
                // However, we can try to parse it if it is a Guid, or if the method supports fallback.
                // We will try to parse if possible.
                if (Guid.TryParse(processId, out var guidProcessId))
                {
                    previousData = await _auditService.ReadExistingDataAsync(tableName, guidProcessId, connection, transaction);
                }

                if (isAuditEnabled)
                {
                    var modifiedBy = properties.ContainsKey("userid")
                        ? properties["userid"]?.ToString() ?? "system"
                        : "system";
                    await _auditService.TryAuditAsync(
                        tableName,
                        Guid.Empty,
                        AuditOperation.UPDATE,
                        modifiedBy,
                        properties,
                        connection,
                        transaction,
                        previousData);
                }
            }

            return (processIdValue, previousData);
        }

        private static object? GetFirstValueExtended(Dictionary<string, object> source, params string[] keys)
        {
            if (source == null || keys == null) return null;
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var match = source.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match.Key)) return match.Value;
            }
            return null;
        }

        private async Task<(object? RenGuid, Dictionary<string, object?>? PreviousData)> CreateMainRecordAsync(
        TransactionRequest request,
        List<FieldMapper> mappers,
        IDbTransaction transaction,
        string transactionId,
        bool isAuditEnabled)
        {
            var parameters = new Dictionary<string, object>();
            var idColumnName = "";
            var idDataType = "";
            var idValue = (object?)null;
            var isUpdate = false;

            // First pass: Check for ID value to determine INSERT vs UPDATE
            foreach (var mapper in mappers.Where(m => m.IsActive))
            {
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    // Store the ID column name and type - this will be the RENGUID
                    idColumnName = mapper.ColumnName;
                    idDataType = mapper.DataType.ToLower();

                    // Check if ID is provided in input (indicates UPDATE)
                    if (request.ExtendedProperties.TryGetValue(mapper.FieldName, out var providedId) &&
                        providedId != null && !string.IsNullOrWhiteSpace(providedId.ToString()))
                    {
                        idValue = _dataTypeConverter.ConvertValue(providedId, mapper.DataType, mapper.DefaultValue, mapper.FieldName, mapper.SequenceName, mapper.Properties);
                        isUpdate = true;
                        _logger.LogInformation("ID provided ({IdValue}), will perform UPDATE operation", idValue);
                    }
                    break; // Only need to find the ID field
                }
            }

            if (!isUpdate && idValue == null && HasValidRequestTransactionId(request))
            {
                idValue = _dataTypeConverter.ConvertValue(request.TransactionId, idDataType, null, idColumnName, null, "AutoGenerated");
                isUpdate = true;
                _logger.LogInformation("Using transactionId ({IdValue}) as ID for UPDATE operation", idValue);
            }

            // Second pass: Build parameters based on operation type
            foreach (var mapper in mappers.Where(m => m.IsActive))
            {
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    // Skip ID field for parameter building
                    continue;
                }

                // For UPDATE operations, check AllowUpdate flag
                if (isUpdate && !mapper.AllowUpdate)
                {
                    _logger.LogDebug("Skipping field {FieldName} in UPDATE operation (AllowUpdate = false)", mapper.FieldName);
                    continue;
                }

                var value = await GetFieldValue(request.ExtendedProperties, mapper, null, transactionId);
                if (value != null)
                {
                    if (mapper.DataType.Equals("jsonb", StringComparison.OrdinalIgnoreCase) ||
                       mapper.DataType.Equals("json", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters[mapper.ColumnName] = $"SQL:to_jsonb('{value}'::jsonb)";
                    }
                    else
                    {
                        parameters[mapper.ColumnName] = value;
                    }
                }
            }

            object? actualRenGuid;
            Dictionary<string, object?>? previousData = null;

            if (isUpdate && idValue != null)
            {
                // UPDATE operation
                var whereConditions = new Dictionary<string, object> { { idColumnName, idValue } };

                // AUDIT: Call BEFORE update to capture existing data (only if enabled or to gather previous data)
                if (_auditService != null && parameters.Any())
                {
                    // Convert idValue to Guid for the read operation
                    Guid guidId = idValue is Guid guid
                    ? guid
                    : Guid.TryParse(idValue?.ToString(), out var parsedGuid)
                        ? parsedGuid
                        : Guid.Empty;
                    if (guidId != Guid.Empty)
                    {
                        previousData = await _auditService.ReadExistingDataAsync(request.TransactionEntityName, guidId, transaction.Connection, transaction, default, idColumnName);

                        if (isAuditEnabled)
                        {
                            var modifiedBy = request.ExtendedProperties.ContainsKey("userid")
                                ? request.ExtendedProperties["userid"]?.ToString() ?? "system"
                                : "system";

                            await _auditService.TryAuditAsync(
                                request.TransactionEntityName,
                                guidId,
                                AuditOperation.UPDATE,
                                modifiedBy,
                                request.ExtendedProperties,
                                transaction.Connection,
                                transaction,
                                previousData);
                        }
                    }
                }

                if (parameters.Any())
                {
                    var rowsAffected = await _transactionDataProvider.ExecuteUpdateAsync(
                        request.TransactionEntityName, parameters, whereConditions, transaction);

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"No record found with {idColumnName} = {idValue} for update");
                    }
                }

                actualRenGuid = idValue;
                _logger.LogInformation("Updated main record with RENGUID {RenGuid} (Type: {RenGuidType}) for transaction {TransactionId}",
                    actualRenGuid, actualRenGuid.GetType().Name, transactionId);
            }
            else
            {
                // INSERT operation
                if (!string.IsNullOrEmpty(idColumnName))
                {
                    var result = await _transactionDataProvider.ExecuteInsertWithReturnAsync(
                        request.TransactionEntityName, parameters, idColumnName, transaction);

                    if (result != null)
                    {
                        actualRenGuid = result;
                    }
                    else
                    {
                        // Fallback based on data type
                        actualRenGuid = idDataType switch
                        {
                            "int" => 0,
                            "guid" => Guid.NewGuid(),
                            "string" => Guid.NewGuid().ToString(),
                            _ => Guid.NewGuid()
                        };
                    }
                }
                else
                {
                    // Fallback: regular insert if no AutoGenerated ID field
                    var formattedTableName = _transactionDataProvider.FormatTableName(request.TransactionEntityName);
                    var columns = parameters.Keys.ToList();
                    var placeholders = columns.Select(col => _transactionDataProvider.GetParameterPlaceholder(col)).ToList();
                    var sql = $"INSERT INTO {formattedTableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", placeholders)})";

                    await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, transaction);
                    actualRenGuid = Guid.NewGuid(); // Fallback GUID
                }

                //if (request.IsApprovalCreation)
                //    await CreateApprovalRecord((Guid)actualRenGuid, request);

                // AUDIT: Fire-and-forget audit for CREATE operation (only if enabled)
                if (_auditService != null && isAuditEnabled)
                {
                    var modifiedBy = request.ExtendedProperties.ContainsKey("userid")
                        ? request.ExtendedProperties["userid"]?.ToString() ?? "system"
                        : "system";

                    // Convert actualRenGuid to Guid
                    Guid guidId = idValue is Guid guid
                    ? guid
                    : Guid.TryParse(idValue?.ToString(), out var parsedGuid)
                        ? parsedGuid
                        : Guid.Empty;
                    if (guidId != Guid.Empty)
                    {
                        _ = _auditService.TryAuditAsync(
                        request.TransactionEntityName,
                        guidId,
                        AuditOperation.CREATE,
                        modifiedBy,
                        request.ExtendedProperties,
                        transaction.Connection,
                        transaction);
                    }
                }

                _logger.LogInformation("Created main record with RENGUID {RenGuid} (Type: {RenGuidType}) for transaction {TransactionId}",
                    actualRenGuid, actualRenGuid.GetType().Name, transactionId);
            }

            return (actualRenGuid, previousData);
        }

        private bool HasValidRequestTransactionId(TransactionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return false;
            }

            var transactionId = request.TransactionId.Trim();
            return transactionId != "00000000-0000-0000-0000-000000000000" && transactionId != "0";
        }

        private void EnsureRequestIdFromTransactionId(TransactionRequest request)
        {
            if (!HasValidRequestTransactionId(request))
            {
                return;
            }

            if (request.ExtendedProperties == null)
            {
                request.ExtendedProperties = new Dictionary<string, object>();
            }

            if (!request.ExtendedProperties.TryGetValue("id", out var idValue) ||
                idValue == null ||
                string.IsNullOrWhiteSpace(idValue.ToString()) ||
                idValue.ToString() == "00000000-0000-0000-0000-000000000000" ||
                idValue.ToString() == "0")
            {
                request.ExtendedProperties["id"] = request.TransactionId;
            }
        }

        //private async Task<object> CreateMainRecordAsync(
        //    TransactionRequest request, 
        //    List<FieldMapper> mappers, 
        //    IDbConnection connection,
        //    IDbTransaction transaction, 
        //    string transactionId)
        //{
        //    _logger.LogInformation("Creating main record using field mappers for entity {EntityName}", request.TransactionEntityName);

        //    try
        //    {
        //        var operationType = DetermineOperationType(request, mappers);
        //        var tableName = request.TransactionEntityName;

        //        if (operationType == "Update")
        //        {
        //            return await UpdateMainRecordAsync(request, mappers, connection, transaction, transactionId);
        //        }

        //        // INSERT operation
        //        var columns = new List<string>();
        //        var values = new List<string>();
        //        var parameters = new Dictionary<string, object>();
        //        var renGuid = Guid.NewGuid();

        //        // Process each field mapper
        //        foreach (var mapper in mappers)
        //        {
        //            var fieldValue = await GetFieldValue(request.ExtendedProperties, mapper, renGuid, transactionId);

        //            // Handle auto-generated fields
        //            if (mapper.Properties.Contains("AutoGenerated") && operationType == "Insert")
        //            {
        //                if (mapper.Properties.Contains("|Sequence|"))
        //                {
        //                    // Handle sequence-based auto generation
        //                    var sequencePattern = ExtractSequencePattern(mapper.Properties);
        //                    if (!string.IsNullOrEmpty(sequencePattern))
        //                    {
        //                        fieldValue = await AutoNumberService.GenerateAutoNumberAsync(sequencePattern, _transactionDataProvider);
        //                    }
        //                }
        //                else if (mapper.DataType.ToLower().Contains("guid") || mapper.DataType.ToLower().Contains("uuid"))
        //                {
        //                    fieldValue = renGuid;
        //                }
        //            }

        //            // Convert value based on data type
        //            var convertedValue = _dataTypeConverter.ConvertValue(
        //                fieldValue, 
        //                mapper.DataType, 
        //                mapper.DefaultValue, 
        //                mapper.FieldName, 
        //                null, 
        //                mapper.Properties);

        //            if (convertedValue != null)
        //            {
        //                columns.Add(mapper.ColumnName);
        //                values.Add($"@{mapper.ColumnName}");
        //                parameters[mapper.ColumnName] = convertedValue;
        //            }
        //        }

        //        // Add audit fields if not already included
        //        if (!columns.Contains("createdon"))
        //        {
        //            columns.Add("createdon");
        //            values.Add("@createdon");
        //            parameters["createdon"] = DateTime.UtcNow;
        //        }

        //        if (request.ExtendedProperties.ContainsKey("userid") && !columns.Contains("createdby"))
        //        {
        //            columns.Add("createdby");
        //            values.Add("@createdby");
        //            parameters["createdby"] = request.ExtendedProperties["userid"];
        //        }

        //        var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

        //        _logger.LogDebug("Executing INSERT SQL: {Sql}", sql);

        //        await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

        //        _logger.LogInformation("Successfully created main record with ID {RenGuid}", renGuid);
        //        return renGuid;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to create main record for entity {EntityName}", request.TransactionEntityName);
        //        throw;
        //    }
        //}

        private async Task<object> UpdateMainRecordAsync(
            TransactionRequest request,
            List<FieldMapper> mappers,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId)
        {
            _logger.LogInformation("Updating main record using field mappers for entity {EntityName}", request.TransactionEntityName);

            try
            {
                var tableName = request.TransactionEntityName;
                var idMapper = mappers.FirstOrDefault(m => m.Properties.Contains("AutoGenerated"));

                if (idMapper == null)
                {
                    throw new InvalidOperationException("No ID field mapper found for update operation");
                }

                if (!request.ExtendedProperties.TryGetValue(idMapper.FieldName, out var idValue) || idValue == null)
                {
                    throw new InvalidOperationException($"ID field '{idMapper.FieldName}' is required for update operations");
                }

                var setClauses = new List<string>();
                var parameters = new Dictionary<string, object>();
                parameters[idMapper.ColumnName] = idValue;

                // Process each field mapper (except ID field)
                foreach (var mapper in mappers.Where(m => !m.Properties.Contains("AutoGenerated")))
                {
                    // Check if field allows updates
                    if (!mapper.AllowUpdate)
                    {
                        _logger.LogDebug("Skipping field {FieldName} - updates not allowed", mapper.FieldName);
                        continue;
                    }

                    var fieldValue = await GetFieldValue(request.ExtendedProperties, mapper, idValue, transactionId);

                    // Convert value based on data type
                    var convertedValue = _dataTypeConverter.ConvertValue(
                        fieldValue,
                        mapper.DataType,
                        mapper.DefaultValue,
                        mapper.FieldName,
                        null,
                        mapper.Properties);

                    if (convertedValue != null)
                    {
                        setClauses.Add($"{mapper.ColumnName} = @{mapper.ColumnName}");
                        parameters[mapper.ColumnName] = convertedValue;
                    }
                }

                // Add audit fields
                setClauses.Add("modifiedon = @modifiedon");
                parameters["modifiedon"] = DateTime.UtcNow;

                if (request.ExtendedProperties.ContainsKey("userid"))
                {
                    setClauses.Add("modifiedby = @modifiedby");
                    parameters["modifiedby"] = request.ExtendedProperties["userid"];
                }

                if (setClauses.Count == 0)
                {
                    _logger.LogWarning("No fields to update for entity {EntityName}", request.TransactionEntityName);
                    return idValue;
                }

                var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {idMapper.ColumnName} = @{idMapper.ColumnName}";

                _logger.LogDebug("Executing UPDATE SQL: {Sql}", sql);

                var rowsAffected = await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"No record found with ID {idValue} to update");
                }

                _logger.LogInformation("Successfully updated main record with ID {Id}", idValue);
                return idValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update main record for entity {EntityName}", request.TransactionEntityName);
                throw;
            }
        }

        private async Task ProcessChildRecordsModelBindingAsync(
            TransactionRequest request,
            object renGuid,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            Dictionary<string, List<TriggerEntry>> triggersCache,
            bool isMainAuditEnabled,
            Dictionary<string, bool> auditEnabledCache)
        {
            _logger.LogInformation("Processing child records using model binding for transaction {TransactionId}", transactionId);

            try
            {
                if (request.RenProps == null || !request.RenProps.Any())
                {
                    _logger.LogDebug("No child records to process");
                    return;
                }

                foreach (var childEntity in request.RenProps)
                {
                    var childTableName = childEntity.Key;
                    var childRecords = childEntity.Value;

                    // Check if audit is enabled for THIS child table (cached per table)
                    bool isChildAuditEnabled = await IsAuditEnabledCachedAsync(childTableName, connection, transaction, auditEnabledCache);

                    _logger.LogInformation("Processing {Count} records for child table {TableName} (Audit: {AuditEnabled})",
                        childRecords.Count, childTableName, isChildAuditEnabled);

                    foreach (var childRecord in childRecords)
                    {
                        await ProcessSingleChildRecordModelBindingAsync(
                            childTableName,
                            childRecord,
                            renGuid,
                            connection,
                            transaction,
                            transactionId,
                            triggersCache,
                            isChildAuditEnabled);
                    }
                }

                _logger.LogInformation("Successfully processed all child records for transaction {TransactionId}", transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process child records for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        private async Task ProcessSingleChildRecordModelBindingAsync(
            string tableName,
            Dictionary<string, object> recordData,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            Dictionary<string, List<TriggerEntry>> triggersCache,
            bool isAuditEnabled)
        {
            try
            {
                var operationType = recordData.ContainsKey("id") &&
                                  recordData["id"] != null &&
                                  !string.IsNullOrWhiteSpace(recordData["id"].ToString()) &&
                                  recordData["id"].ToString() != "00000000-0000-0000-0000-000000000000"
                    ? "Update" : "Insert";

                if (operationType == "Insert")
                {
                    await CreateChildRecordModelBindingAsync(tableName, recordData, parentId, connection, transaction, transactionId, isAuditEnabled);
                }
                else
                {
                    await UpdateChildRecordModelBindingAsync(tableName, recordData, parentId, connection, transaction, transactionId, isAuditEnabled);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process child record in table {TableName}", tableName);
                throw;
            }
        }

        private async Task CreateChildRecordModelBindingAsync(
            string tableName,
            Dictionary<string, object> recordData,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new Dictionary<string, object>();
            var childId = Guid.NewGuid();

            // Add child ID
            columns.Add("id");
            values.Add("@id");
            parameters["id"] = childId;

            // Process all properties
            foreach (var prop in recordData)
            {
                if (prop.Key.ToLower() == "id") continue; // Skip ID for insert

                var columnName = prop.Key;
                var paramName = $"@{columnName}";

                columns.Add(columnName);
                values.Add(paramName);

                // Handle special values
                if (prop.Value?.ToString() == "|RENGUID|")
                {
                    parameters[columnName] = parentId;
                }
                else if (prop.Value?.ToString()?.ToLower() == "now")
                {
                    parameters[columnName] = DateTime.UtcNow;
                }
                else
                {
                    parameters[columnName] = prop.Value ?? DBNull.Value;
                }
            }

            if (!columns.Any(c => c.Equals("createdon", StringComparison.OrdinalIgnoreCase)))
            {
                columns.Add("createdon");
                values.Add("@createdon");
                parameters["createdon"] = DateTime.UtcNow;
            }

            var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

            _logger.LogDebug("Executing child INSERT SQL: {Sql}", sql);

            await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

            _logger.LogDebug("Successfully created child record in {TableName} with ID {ChildId}", tableName, childId);

            // AUDIT: Fire-and-forget audit for child CREATE operation (only if enabled)
            if (_auditService != null && isAuditEnabled)
            {
                var modifiedBy = recordData.ContainsKey("userid")
                    ? recordData["userid"]?.ToString() ?? "system"
                    : "system";

                _ = _auditService.TryAuditAsync(
                    tableName,
                    childId,
                    AuditOperation.CREATE,
                    modifiedBy,
                    recordData,
                    connection,
                    transaction);
            }
        }

        private async Task UpdateChildRecordModelBindingAsync(
            string tableName,
            Dictionary<string, object> recordData,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled)
        {
            if (!recordData.TryGetValue("id", out var idValue) || idValue == null)
            {
                throw new InvalidOperationException("ID is required for child record update operations");
            }

            var childId = Guid.Parse(idValue.ToString());
            var modifiedBy = recordData.ContainsKey("userid")
                ? recordData["userid"]?.ToString() ?? "system"
                : "system";

            // AUDIT: Call BEFORE update to capture existing data (only if enabled)
            if (_auditService != null)
            {
                var previousData = await _auditService.ReadExistingDataAsync(tableName, childId, connection, transaction);
                if (isAuditEnabled)
                {
                    await _auditService.TryAuditAsync(
                        tableName,
                        childId,
                        AuditOperation.UPDATE,
                        modifiedBy,
                        recordData,
                        connection,
                        transaction,
                        previousData);
                }
            }

            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>();
            parameters["id"] = childId;

            // Process all properties except ID
            foreach (var prop in recordData)
            {
                if (prop.Key.ToLower() == "id") continue; // Skip ID for update

                var columnName = prop.Key;
                var paramName = $"@{columnName}";

                setClauses.Add($"{columnName} = {paramName}");

                // Handle special values
                if (prop.Value?.ToString() == "|RENGUID|")
                {
                    parameters[columnName] = parentId;
                }
                else if (prop.Value?.ToString()?.ToLower() == "now")
                {
                    parameters[columnName] = DateTime.UtcNow;
                }
                else
                {
                    parameters[columnName] = prop.Value ?? DBNull.Value;
                }
            }

            // Add audit fields
            setClauses.Add("modifiedon = @modifiedon");
            parameters["modifiedon"] = DateTime.UtcNow;

            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE id = @id";

            _logger.LogDebug("Executing child UPDATE SQL: {Sql}", sql);

            var rowsAffected = await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"No child record found with ID {idValue} to update in table {tableName}");
            }

            _logger.LogDebug("Successfully updated child record in {TableName} with ID {Id}", tableName, idValue);
        }

        private async Task ProcessChildRecordsAsync(
    Dictionary<string, List<Dictionary<string, object>>> renProps,
    object? parentRenGuid,
    IDbTransaction transaction,
    string transactionId,
    int currentDepth,
    Dictionary<string, List<TriggerEntry>> _trggersCache,
    Dictionary<string, bool> auditEnabledCache,
    Dictionary<string, List<FieldMapper>> mappersCache)
        {
            const int MAX_DEPTH = 5;

            if (currentDepth > MAX_DEPTH)
            {
                _logger.LogWarning("Maximum recursion depth ({MaxDepth}) reached for transaction {TransactionId}. Skipping deeper child records.",
                    MAX_DEPTH, transactionId);
                return;
            }

            foreach (var renProp in renProps)
            {
                var childTableName = renProp.Key;
                var childRecords = renProp.Value;

                // Check if audit is enabled for THIS child table (cached per table)
                bool isChildAuditEnabled = await IsAuditEnabledCachedAsync(childTableName, transaction.Connection!, transaction, auditEnabledCache);

                _logger.LogInformation("Processing {Count} records for child table {TableName} at depth {Depth} (Audit: {AuditEnabled})",
                    childRecords.Count, childTableName, currentDepth, isChildAuditEnabled);

                var childMappers = await GetFieldMappersCachedAsync(childTableName, transaction, mappersCache);

                foreach (var childRecord in childRecords)
                {
                    var resultTuple = await CreateChildRecordAsync(childTableName, childRecord, childMappers,
                        parentRenGuid, transaction, transactionId, currentDepth, isChildAuditEnabled);

                    var childRecordId = resultTuple.ChildRecordId;
                    var previousData = resultTuple.PreviousData;

                    // Process nested RenProps recursively
                    if (childRecordId != null && childRecord.ContainsKey("RenProps"))
                    {
                        var nestedRenProps = childRecord["RenProps"];

                        if (nestedRenProps is Dictionary<string, List<Dictionary<string, object>>> nestedDict)
                        {
                            _logger.LogInformation("Processing nested RenProps at depth {Depth} for child table {TableName}",
                                currentDepth + 1, childTableName);

                            await ProcessChildRecordsAsync(nestedDict, childRecordId, transaction, transactionId, currentDepth + 1, _trggersCache, auditEnabledCache, mappersCache);
                        }
                        else if (nestedRenProps != null)
                        {
                            // Try to deserialize if it's a JSON string or JObject
                            try
                            {
                                var jsonString = nestedRenProps.ToString();
                                var deserializedRenProps = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonString);

                                if (deserializedRenProps != null)
                                {
                                    _logger.LogInformation("Processing nested RenProps (deserialized) at depth {Depth} for child table {TableName}",
                                        currentDepth + 1, childTableName);

                                    await ProcessChildRecordsAsync(deserializedRenProps, childRecordId, transaction, transactionId, currentDepth + 1, _trggersCache, auditEnabledCache, mappersCache);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize nested RenProps for child table {TableName}", childTableName);
                            }
                        }
                    }

                    // Trigger DAGs for child record operations (fire and forget)
                    if (childRecordId != null)
                    {
                        try
                        {
                            // Determine operation type for child record
                            var childOperationType = DetermineChildOperationType(childRecord, childMappers);

                            // Get triggers for this child table and operation
                            var childTriggers = await DagTriggerRepository.GetTriggersForTableAsync(
                                childTableName,
                                childOperationType);

                            if (childTriggers.Any())
                            {
                                _logger.LogInformation(
                                    "Found {TriggerCount} DAG triggers for child table {TableName}, operation {OperationType}",
                                    childTriggers.Count, childTableName, childOperationType);

                                // Prepare context data for DAG
                                var childDagContext = new Dictionary<string, object>(childRecord)
                                {
                                    { "operationType", childOperationType },
                                    { "transactionId", transactionId },
                                    { "parentRecordId", parentRenGuid }
                                };

                                if (previousData != null)
                                {
                                    childDagContext["previousData"] = previousData;
                                }

                                // Trigger each DAG asynchronously (fire and forget)
                                foreach (var trigger in childTriggers)
                                {
                                    // Create logging callback
                                    Func<string, string?, string?, int?, int?, Task> onChildExecutionComplete = async (status, responseData, errorMessage, httpStatusCode, executionTimeMs) =>
                                    {
                                        try
                                        {
                                            // Log the trigger execution
                                            await DagTriggerRepository.LogTriggerExecutionAsync(
                                                trigger.Id,
                                                trigger.DagId,
                                                childTableName,
                                                childOperationType,
                                                childRecordId.ToString(),
                                                Newtonsoft.Json.JsonConvert.SerializeObject(childDagContext),
                                                responseData,
                                                status,
                                                errorMessage,
                                                httpStatusCode,
                                                executionTimeMs);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Error logging child DAG trigger execution for DAG {DagId}", trigger.DagId);
                                        }
                                    };

                                    if (!_trggersCache.ContainsKey(childTableName))
                                    {
                                        _trggersCache[childTableName] = new List<TriggerEntry>();
                                    }

                                    _trggersCache[childTableName].Add(new TriggerEntry
                                    {
                                        DagId = trigger.DagId.ToString(),
                                        ChildTableName = childTableName,
                                        ChildRecordId = childRecordId.ToString(),
                                        ChildDagContext = childDagContext,
                                        OnChildExecutionComplete = onChildExecutionComplete
                                    });

                                    //_ = DagTriggerService.TriggerDagAsync(
                                    //    trigger.DagId,
                                    //    childTableName,
                                    //    childRecordId,
                                    //    childDagContext,
                                    //    onChildExecutionComplete);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error triggering DAGs for child table {TableName}", childTableName);
                            // Don't fail the transaction due to trigger errors
                        }
                    }
                }
            }
        }

        private string DetermineChildOperationType(Dictionary<string, object> childRecord, List<FieldMapper> mappers)
        {
            // Check if an ID field is provided in the child record data
            var idMapper = mappers.FirstOrDefault(m => m.Properties.Contains("AutoGenerated"));

            if (idMapper != null && childRecord.TryGetValue(idMapper.FieldName, out var idValue))
            {
                var stringValue = idValue?.ToString();

                // Check if it's a valid ID (not a placeholder or empty)
                if (!string.IsNullOrWhiteSpace(stringValue) &&
                    stringValue != "|RENGUID|" &&
                    stringValue != "|Todaydate|")
                {
                    return "Update";
                }
            }

            return "Insert";
        }

        private async Task<(object? ChildRecordId, Dictionary<string, object?>? PreviousData)> CreateChildRecordAsync(
            string tableName,
            Dictionary<string, object> recordData,
            List<FieldMapper> mappers,
            object renGuid,
            IDbTransaction transaction,
            string transactionId,
            int currentDepth = 1,
            bool isAuditEnabled = false)
        {
            var parameters = new Dictionary<string, object>();
            var idColumnName = "";
            var idValue = (object?)null;
            var isUpdate = false;
            object? childRecordId = null;
            Dictionary<string, object?>? previousData = null;
            var idDataType = "";

            _logger.LogInformation("Creating child record in {TableName} at depth {Depth}", tableName, currentDepth);

            // First pass: Check for ID value to determine INSERT vs UPDATE
            foreach (var mapper in mappers.Where(m => m.IsActive))
            {
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    idColumnName = mapper.ColumnName;
                    idDataType = mapper.DataType.ToLower();

                    // Check if ID is provided in child record data (indicates UPDATE)
                    if (recordData.TryGetValue(mapper.FieldName, out var providedId) &&
                        providedId != null && !string.IsNullOrWhiteSpace(providedId.ToString()) &&
                        providedId.ToString() != "|RENGUID|" && providedId.ToString() != "|Todaydate|")
                    {
                        idValue = _dataTypeConverter.ConvertValue(providedId, mapper.DataType, mapper.DefaultValue, mapper.FieldName, mapper.SequenceName, mapper.Properties);
                        isUpdate = true;
                        _logger.LogInformation("Child record ID provided ({IdValue}), will perform UPDATE operation", idValue);
                    }
                    break; // Only need to find the ID field
                }
            }

            // Second pass: Build parameters based on operation type
            foreach (var mapper in mappers.Where(m => m.IsActive))
            {
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    // Skip ID field for parameter building
                    continue;
                }

                // For UPDATE operations, check AllowUpdate flag
                if (isUpdate && !mapper.AllowUpdate)
                {
                    _logger.LogDebug("Skipping field {FieldName} in child UPDATE operation (AllowUpdate = false)", mapper.FieldName);
                    continue;
                }

                // Skip RenProps field - it's for nested processing, not a database column
                if (mapper.FieldName.Equals("RenProps", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = await GetChildFieldValue(recordData, mapper, renGuid, transactionId);
                if (value != null)
                {
                    parameters[mapper.ColumnName] = value;
                }
            }

            if (!parameters.Any() && !isUpdate)
            {
                _logger.LogWarning("No valid fields found for child record in {TableName}", tableName);
                return (null, null);
            }

            if (isUpdate && idValue != null)
            {
                // UPDATE operation for child record
                var whereConditions = new Dictionary<string, object> { { idColumnName, idValue } };

                // AUDIT: Call BEFORE update to capture existing data (only if enabled or needed)
                if (_auditService != null && parameters.Any())
                {
                    // Convert idValue to Guid
                    Guid guidId = idValue is Guid guid
                    ? guid
                    : Guid.TryParse(idValue?.ToString(), out var parsedGuid)
                        ? parsedGuid
                        : Guid.Empty;
                    if (guidId != Guid.Empty)
                    {
                        previousData = await _auditService.ReadExistingDataAsync(tableName, guidId, transaction.Connection, transaction, default, idColumnName);

                        if (isAuditEnabled)
                        {
                            var modifiedBy = recordData.ContainsKey("userid")
                                ? recordData["userid"]?.ToString() ?? "system"
                                : "system";

                            await _auditService.TryAuditAsync(
                                tableName,
                                guidId,
                                AuditOperation.UPDATE,
                                modifiedBy,
                                recordData,
                                transaction.Connection,
                                transaction,
                                previousData);
                        }
                    }
                }

                if (parameters.Any())
                {
                    var rowsAffected = await _transactionDataProvider.ExecuteUpdateAsync(
                        tableName, parameters, whereConditions, transaction);

                    if (rowsAffected > 0)
                        childRecordId = idValue;

                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("No child record found with {IdColumn} = {IdValue} for update in {TableName}",
                            idColumnName, idValue, tableName);
                    }
                }
                else
                {
                    _logger.LogInformation("Updated child record in {TableName} for transaction {TransactionId}",
                        tableName, transactionId);
                    childRecordId = idValue;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(idColumnName))
                {
                    var result = await _transactionDataProvider.ExecuteInsertWithReturnAsync(
                        tableName, parameters, idColumnName, transaction);

                    if (result != null)
                    {
                        childRecordId = result;
                    }
                    else
                    {
                        // Fallback based on data type
                        childRecordId = idDataType switch
                        {
                            "int" => 0,
                            "guid" => Guid.NewGuid(),
                            "string" => Guid.NewGuid().ToString(),
                            _ => Guid.NewGuid()
                        };
                    }
                }
                else
                {
                    // INSERT operation for child record
                    var formattedTableName = _transactionDataProvider.FormatTableName(tableName);
                    var columns = parameters.Keys.ToList();
                    var placeholders = columns.Select(col => _transactionDataProvider.GetParameterPlaceholder(col)).ToList();
                    var sql = $"INSERT INTO {formattedTableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", placeholders)})";

                    await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, transaction);

                    _logger.LogInformation("Created child record in {TableName} for transaction {TransactionId}",
                        tableName, transactionId);

                    // Try to get the inserted record ID if available
                    if (!string.IsNullOrEmpty(idColumnName))
                    {
                        childRecordId = idValue ?? Guid.NewGuid();
                    }
                }

                // AUDIT: Fire-and-forget audit for child CREATE operation (only if enabled)
                if (_auditService != null && isAuditEnabled && childRecordId != null)
                {
                    var modifiedBy = recordData.ContainsKey("userid")
                        ? recordData["userid"]?.ToString() ?? "system"
                        : "system";

                    // Convert childRecordId to Guid
                    Guid guidId = idValue is Guid guid
                    ? guid
                    : Guid.TryParse(idValue?.ToString(), out var parsedGuid)
                        ? parsedGuid
                        : Guid.Empty;
                    if (guidId != Guid.Empty)
                    {

                        _ = _auditService.TryAuditAsync(
                        tableName,
                        guidId,
                        AuditOperation.CREATE,
                        modifiedBy,
                        recordData,
                        transaction.Connection,
                        transaction);
                    }
                }
            }

            return (childRecordId, previousData);
        }

        private async Task ProcessChildRecordsAsync(
            TransactionRequest request,
            object renGuid,
            IDbTransaction transaction,
            string transactionId,
            Dictionary<string, List<TriggerEntry>> _trggersCache,
            bool isMainAuditEnabled,
            Dictionary<string, bool> auditEnabledCache,
            Dictionary<string, List<FieldMapper>> mappersCache)
        {
            await ProcessChildRecordsAsync(request.RenProps, renGuid, transaction, transactionId, 1, _trggersCache, auditEnabledCache, mappersCache);
        }


        //private async Task ProcessChildRecordsAsync(
        //    TransactionRequest request, 
        //    object renGuid, 
        //    IDbConnection connection,
        //    IDbTransaction transaction, 
        //    string transactionId, 
        //    Dictionary<string, List<TriggerEntry>> triggersCache)
        //{
        //    _logger.LogInformation("Processing child records using field mappers for transaction {TransactionId}", transactionId);

        //    try
        //    {
        //        if (request.RenProps == null || !request.RenProps.Any())
        //        {
        //            _logger.LogDebug("No child records to process");
        //            return;
        //        }

        //        foreach (var childEntity in request.RenProps)
        //        {
        //            var childTableName = childEntity.Key;
        //            var childRecords = childEntity.Value;

        //            _logger.LogInformation("Processing {Count} records for child table {TableName}", 
        //                childRecords.Count, childTableName);

        //            // Get field mappers for child table
        //            var childMappers = await _fieldMapperService.GetFieldMappersAsync(childTableName);

        //            if (!childMappers.Any())
        //            {
        //                _logger.LogWarning("No field mappers found for child entity: {EntityName}", childTableName);
        //                continue;
        //            }

        //            foreach (var childRecord in childRecords)
        //            {
        //                await ProcessSingleChildRecordAsync(
        //                    childTableName, 
        //                    childRecord, 
        //                    childMappers,
        //                    renGuid, 
        //                    connection, 
        //                    transaction, 
        //                    transactionId,
        //                    triggersCache);
        //            }
        //        }

        //        _logger.LogInformation("Successfully processed all child records for transaction {TransactionId}", transactionId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to process child records for transaction {TransactionId}", transactionId);
        //        throw;
        //    }
        //}

        private async Task ProcessSingleChildRecordAsync(
            string tableName,
            Dictionary<string, object> recordData,
            List<FieldMapper> mappers,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            Dictionary<string, List<TriggerEntry>> triggersCache)
        {
            try
            {
                var idMapper = mappers.FirstOrDefault(m => m.Properties.Contains("AutoGenerated"));
                var operationType = "Insert";

                if (idMapper != null && recordData.TryGetValue(idMapper.FieldName, out var idValue) &&
                    idValue != null && !string.IsNullOrWhiteSpace(idValue.ToString()))
                {
                    operationType = "Update";
                }

                if (operationType == "Insert")
                {
                    await CreateChildRecordAsync(tableName, recordData, mappers, parentId, connection, transaction, transactionId);
                }
                else
                {
                    await UpdateChildRecordAsync(tableName, recordData, mappers, parentId, connection, transaction, transactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process child record in table {TableName}", tableName);
                throw;
            }
        }

        private async Task<object?> GetChildFieldValue(
            Dictionary<string, object> recordData,
            FieldMapper mapper,
            object renGuid,
            string transactionId)
        {
            object? value = null;

            if (recordData.TryGetValue(mapper.FieldName, out var inputValue))
            {
                var stringValue = inputValue?.ToString();

                // Replace placeholders
                if (stringValue == "|RENGUID|")
                    return renGuid;

                if (stringValue == "|Todaydate|")
                    return DateTime.Now;

                value = inputValue;
            }

            // Handle special properties
            if (mapper.Properties.Contains("Default today date"))
                return DateTime.Now;

            // Handle Sequence-based auto-number generation
            if (mapper.Properties.Contains("Sequence") && !string.IsNullOrEmpty(mapper.SequenceName))
            {
                try
                {
                    _logger.LogInformation("Generating auto-number for child field {FieldName} using sequence pattern {SequenceName}",
                        mapper.FieldName, mapper.SequenceName);

                    var autoNumber = await AutoNumberService.GenerateAutoNumberAsync(mapper.SequenceName, _transactionDataProvider);
                    _logger.LogInformation("Generated auto-number: {AutoNumber} for child field {FieldName}", autoNumber, mapper.FieldName);
                    return autoNumber;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating auto-number for child field {FieldName}", mapper.FieldName);
                    throw;
                }
            }

            // Convert value using comprehensive type converter with default value support and auto-generation
            return _dataTypeConverter.ConvertValue(value, mapper.DataType, mapper.DefaultValue, mapper.FieldName, mapper.SequenceName, mapper.Properties);
        }

        private async Task CreateChildRecordAsync(
            string tableName,
            Dictionary<string, object> recordData,
            List<FieldMapper> mappers,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled = false)
        {
            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new Dictionary<string, object>();
            var childId = Guid.NewGuid();

            // Process each field mapper
            foreach (var mapper in mappers)
            {
                var fieldValue = recordData.TryGetValue(mapper.FieldName, out var value) ? value : null;

                // Handle auto-generated fields
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    if (mapper.Properties.Contains("|Sequence|"))
                    {
                        var sequencePattern = ExtractSequencePattern(mapper.Properties);
                        if (!string.IsNullOrEmpty(sequencePattern))
                        {
                            fieldValue = await AutoNumberService.GenerateAutoNumberAsync(sequencePattern, _transactionDataProvider);
                        }
                    }
                    else if (mapper.DataType.ToLower().Contains("guid") || mapper.DataType.ToLower().Contains("uuid"))
                    {
                        fieldValue = childId;
                    }
                }

                // Handle parent reference
                if (fieldValue?.ToString() == "|RENGUID|")
                {
                    fieldValue = parentId;
                }

                // Convert value based on data type
                var convertedValue = _dataTypeConverter.ConvertValue(
                    fieldValue,
                    mapper.DataType,
                    mapper.DefaultValue,
                    mapper.FieldName,
                    null,
                    mapper.Properties);

                if (convertedValue != null)
                {
                    columns.Add(mapper.ColumnName);
                    values.Add($"@{mapper.ColumnName}");
                    parameters[mapper.ColumnName] = convertedValue;
                }
            }

            // Add audit fields if not already included
            if (!columns.Contains("createdon"))
            {
                columns.Add("createdon");
                values.Add("@createdon");
                parameters["createdon"] = DateTime.UtcNow;
            }

            var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

            _logger.LogDebug("Executing child INSERT SQL: {Sql}", sql);

            await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

            _logger.LogDebug("Successfully created child record in {TableName} with ID {ChildId}", tableName, childId);

            // AUDIT: Fire-and-forget audit for child CREATE operation (only if enabled)
            if (_auditService != null && isAuditEnabled)
            {
                var modifiedBy = recordData.ContainsKey("userid")
                    ? recordData["userid"]?.ToString() ?? "system"
                    : "system";

                _ = _auditService.TryAuditAsync(
                    tableName,
                    childId,
                    AuditOperation.CREATE,
                    modifiedBy,
                    recordData,
                    connection,
                    transaction);
            }
        }

        private async Task UpdateChildRecordAsync(
            string tableName,
            Dictionary<string, object> recordData,
            List<FieldMapper> mappers,
            object parentId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId,
            bool isAuditEnabled = false)
        {
            var idMapper = mappers.FirstOrDefault(m => m.Properties.Contains("AutoGenerated"));

            if (idMapper == null)
            {
                throw new InvalidOperationException($"No ID field mapper found for child table {tableName}");
            }

            if (!recordData.TryGetValue(idMapper.FieldName, out var idValue) || idValue == null)
            {
                throw new InvalidOperationException($"ID field '{idMapper.FieldName}' is required for child record update operations");
            }

            var modifiedBy = recordData.ContainsKey("userid")
                ? recordData["userid"]?.ToString() ?? "system"
                : "system";

            // AUDIT: Call BEFORE update to capture existing data (only if enabled)
            if (_auditService != null && isAuditEnabled)
            {
                // Convert idValue to Guid
                var guidId = idValue is Guid guid ? guid : Guid.Parse(idValue?.ToString() ?? Guid.Empty.ToString());

                await _auditService.TryAuditAsync(
                    tableName,
                    guidId,
                    AuditOperation.UPDATE,
                    modifiedBy,
                    recordData,
                    connection,
                    transaction);
            }

            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>();
            parameters[idMapper.ColumnName] = idValue;

            // Process each field mapper (except ID field)
            foreach (var mapper in mappers.Where(m => !m.Properties.Contains("AutoGenerated")))
            {
                // Check if field allows updates
                if (!mapper.AllowUpdate)
                {
                    _logger.LogDebug("Skipping field {FieldName} - updates not allowed", mapper.FieldName);
                    continue;
                }

                var fieldValue = recordData.TryGetValue(mapper.FieldName, out var value) ? value : null;

                // Handle parent reference
                if (fieldValue?.ToString() == "|RENGUID|")
                {
                    fieldValue = parentId;
                }

                // Convert value based on data type
                var convertedValue = _dataTypeConverter.ConvertValue(
                    fieldValue,
                    mapper.DataType,
                    mapper.DefaultValue,
                    mapper.FieldName,
                    null,
                    mapper.Properties);

                if (convertedValue != null)
                {
                    setClauses.Add($"{mapper.ColumnName} = @{mapper.ColumnName}");
                    parameters[mapper.ColumnName] = convertedValue;
                }
            }

            // Add audit fields
            setClauses.Add("modifiedon = @modifiedon");
            parameters["modifiedon"] = DateTime.UtcNow;

            if (setClauses.Count == 0)
            {
                _logger.LogWarning("No fields to update for child record in table {TableName}", tableName);
                return;
            }

            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {idMapper.ColumnName} = @{idMapper.ColumnName}";

            _logger.LogDebug("Executing child UPDATE SQL: {Sql}", sql);

            var rowsAffected = await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"No child record found with ID {idValue} to update in table {tableName}");
            }

            _logger.LogDebug("Successfully updated child record in {TableName} with ID {Id}", tableName, idValue);
        }

        private async Task ProcessDeleteRecordsAsync(
            TransactionRequest request,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId)
        {
            _logger.LogInformation("Processing delete operations for transaction {TransactionId}", transactionId);

            try
            {
                if (request.DelProps == null || !request.DelProps.Any())
                {
                    _logger.LogDebug("No delete operations to process");
                    return;
                }

                foreach (var deleteOperation in request.DelProps)
                {
                    var tableName = deleteOperation.Key;
                    var recordsToDelete = deleteOperation.Value;

                    _logger.LogInformation("Deleting {Count} records from table {TableName}",
                        recordsToDelete.Count, tableName);

                    foreach (var recordData in recordsToDelete)
                    {
                        // Extract ID from record data
                        if (recordData.TryGetValue("id", out var recordId) && recordId != null)
                        {
                            await DeleteSingleRecordAsync(tableName, recordId, connection, transaction, transactionId);
                        }
                        else
                        {
                            _logger.LogWarning("No ID found in delete record data for table {TableName}", tableName);
                        }
                    }
                }

                _logger.LogInformation("Successfully processed all delete operations for transaction {TransactionId}", transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process delete operations for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        private async Task DeleteSingleRecordAsync(
            string tableName,
            object recordId,
            IDbConnection connection,
            IDbTransaction transaction,
            string transactionId)
        {
            try
            {
                var recordGuid = Guid.Parse(recordId.ToString());

                // AUDIT: Call BEFORE delete to capture existing data
                if (_auditService != null)
                {
                    await _auditService.TryAuditAsync(
                        tableName,
                        recordGuid,
                        AuditOperation.DELETE,
                        "system", // You may want to pass this from the request
                        null,
                        connection,
                        transaction);
                }

                var sql = $"DELETE FROM {tableName} WHERE id = @id";
                var parameters = new Dictionary<string, object>
                {
                    { "id", recordGuid }
                };

                _logger.LogDebug("Executing DELETE SQL: {Sql} with ID: {RecordId}", sql, recordId);

                var rowsAffected = await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No record found with ID {RecordId} to delete from table {TableName}", recordId, tableName);
                }
                else
                {
                    _logger.LogDebug("Successfully deleted record with ID {RecordId} from table {TableName}", recordId, tableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete record with ID {RecordId} from table {TableName}", recordId, tableName);
                throw;
            }
        }

        private async Task TriggerBackgroundProcessingAsync(
            TransactionRequest request,
            object renGuid,
            string operationType,
            string transactionId,
            Dictionary<string, List<TriggerEntry>> triggersCache,
            Dictionary<string, object?>? previousData = null)
        {
            try
            {
                var triggers = await DagTriggerRepository.GetTriggersForTableAsync(
                 request.TransactionEntityName,
                 operationType);

                if (triggers.Any())
                {
                    _logger.LogInformation(
                        "Found {TriggerCount} DAG triggers for table {TableName}, operation {OperationType}",
                        triggers.Count, request.TransactionEntityName, operationType);

                    // Prepare context data for DAG
                    var dagContext = new Dictionary<string, object>(request.ExtendedProperties)
                            {
                                { "operationType", operationType },
                                { "transactionId", transactionId }
                            };

                    if (previousData != null)
                    {
                        dagContext["previousData"] = previousData;
                    }

                    // Trigger each DAG asynchronously (fire and forget)
                    foreach (var trigger in triggers)
                    {
                        // Create logging callback
                        Func<string, string?, string?, int?, int?, Task> onExecutionComplete = async (status, responseData, errorMessage, httpStatusCode, executionTimeMs) =>
                        {
                            try
                            {
                                // Log the trigger execution
                                await DagTriggerRepository.LogTriggerExecutionAsync(
                                    trigger.Id,
                                    trigger.DagId,
                                    request.TransactionEntityName,
                                    operationType,
                                    renGuid.ToString(),
                                    Newtonsoft.Json.JsonConvert.SerializeObject(dagContext),
                                    responseData,
                                    status,
                                    errorMessage,
                                    httpStatusCode,
                                    executionTimeMs);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error logging DAG trigger execution for DAG {DagId}", trigger.DagId);
                            }
                        };

                        _ = DagTriggerService.TriggerDagAsync(
                            trigger.DagId,
                            request.TransactionEntityName,
                            renGuid,
                            dagContext,
                            onExecutionComplete);
                    }
                }

                if (triggersCache.Any())
                {
                    foreach (var tableTriggers in triggersCache)
                    {
                        foreach (var triggerEntry in tableTriggers.Value)
                        {
                            _ = DagTriggerService.TriggerDagAsync(
                                Guid.Parse(triggerEntry.DagId),
                                triggerEntry.ChildTableName,
                                triggerEntry.ChildRecordId,
                                triggerEntry.ChildDagContext,
                                triggerEntry.OnChildExecutionComplete);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering DAGs for transaction {TransactionId}", transactionId);
                // Don't fail the transaction due to trigger errors
            }
        }

        /// <summary>
        /// Check if audit is enabled for a specific table (with per-request cache to avoid repeated DB calls).
        /// </summary>
        private async Task<bool> IsAuditEnabledCachedAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction,
            Dictionary<string, bool> cache)
        {
            var key = tableName?.ToLowerInvariant() ?? "";
            if (cache.TryGetValue(key, out var cached))
                return cached;
            var result = await IsAuditEnabledAsync(tableName, connection, transaction).ConfigureAwait(false);
            cache[key] = result;
            return result;
        }

        /// <summary>
        /// Check if audit is enabled for a specific table
        /// </summary>
        private async Task<bool> IsAuditEnabledAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            try
            {
                const string sql = @"
                    SELECT enableaudit
                    FROM public.applicationtable
                    WHERE LOWER(tablename) = LOWER(@tableName)
                    LIMIT 1";

                await using var command = new NpgsqlCommand(sql,
                    (NpgsqlConnection)connection,
                    (NpgsqlTransaction)transaction);
                command.Parameters.Add(new NpgsqlParameter("@tableName",
                    System.Data.DbType.String)
                { Value = tableName });

                var result = await command.ExecuteScalarAsync();

                return result != null && result != DBNull.Value && (bool)result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check audit status for {TableName}, assuming disabled", tableName);
                return false; // Default to disabled if check fails
            }
        }

        /// <summary>
        /// Get field mappers for an entity (cached per entity for this request to avoid repeated DB calls).
        /// </summary>
        private async Task<List<FieldMapper>> GetFieldMappersCachedAsync(
            string entityName,
            IDbTransaction transaction,
            Dictionary<string, List<FieldMapper>> cache)
        {
            var key = entityName ?? "";
            if (cache.TryGetValue(key, out var cached))
                return cached;
            var mappers = await _fieldMapperService.GetFieldMappersAsync(entityName, transaction).ConfigureAwait(false);
            cache[key] = mappers;
            return mappers;
        }

        private async Task<object?> GetFieldValue(
   Dictionary<string, object> extendedProperties,
   FieldMapper mapper,
   object? renGuid,
   string transactionId)
        {
            // Skip AutoGenerated fields - they will be handled by the database
            if (mapper.Properties.Contains("AutoGenerated"))
                return null;

            // Handle special properties first
            if (mapper.Properties.Contains("Default today date"))
                return DateTime.Now;

            if (!extendedProperties.ContainsKey(mapper.FieldName) && string.IsNullOrWhiteSpace(mapper.Properties))
                return null;

            // Handle Sequence-based auto-number generation
            if (mapper.Properties.Contains("Sequence") && !string.IsNullOrEmpty(mapper.DefaultValue))
            {
                try
                {
                    _logger.LogInformation("Generating auto-number for field {FieldName} using sequence pattern {SequenceName}",
                        mapper.FieldName, mapper.DefaultValue);

                    var autoNumber = await AutoNumberService.GenerateAutoNumberAsync(mapper.DefaultValue, _transactionDataProvider);
                    _logger.LogInformation("Generated auto-number: {AutoNumber} for field {FieldName}", autoNumber, mapper.FieldName);
                    return autoNumber;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating auto-number for field {FieldName}", mapper.FieldName);
                    throw;
                }
            }

            // Get value from extended properties
            object? value = null;
            if (extendedProperties.TryGetValue(mapper.FieldName, out var inputValue))
            {
                value = inputValue;
            }

            // Convert value using comprehensive type converter with default value support and auto-generation
            return _dataTypeConverter.ConvertValue(value, mapper.DataType, mapper.DefaultValue, mapper.FieldName, mapper.SequenceName, mapper.Properties);
        }

        private string ExtractSequencePattern(string properties)
        {
            // Extract sequence pattern from properties like "|Sequence:SeqName:PATTERN|"
            var match = System.Text.RegularExpressions.Regex.Match(properties, @"\|Sequence:([^|]+)\|");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }

    // Supporting classes that might be needed
    public class TriggerEntry
    {
        public string DagId { get; set; } = string.Empty;
        public string ChildTableName { get; set; } = string.Empty;
        public object ChildRecordId { get; set; } = new object();
        public Dictionary<string, object> ChildDagContext { get; set; } = new();
        public Func<string, string?, string?, int?, int?, Task> OnChildExecutionComplete { get; set; } = null!;
    }
}