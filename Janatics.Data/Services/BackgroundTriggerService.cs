using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using KATCRUDServices.Core.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Simple background job service to replace Hangfire for trigger processing
    /// Processes DAG triggers and direct insert operations in background threads
    /// </summary>
    public static class BackgroundTriggerService
    {
        private static readonly ConcurrentQueue<TriggerJob> _jobQueue = new();
        private static readonly Timer _processingTimer;
        private static readonly HashSet<string> _processedJobs = new();
        private static readonly object _jobLock = new object();
        private static IDataProvider? _dataProvider;
        private static ITransactionService? _transactionService;
        private static ILogger? _logger;
        private static bool _isProcessing = false;

        static BackgroundTriggerService()
        {
            // Process jobs every 1 second
            _processingTimer = new Timer(ProcessJobs, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Initialize the background trigger service with required dependencies
        /// </summary>
        public static void Initialize(IDataProvider dataProvider, ITransactionService transactionService, ILogger logger)
        {
            _dataProvider = dataProvider;
            _transactionService = transactionService;
            _logger = logger;
            
            _logger.LogInformation("[BackgroundTriggerService] Initialized with dependencies");
        }

        /// <summary>
        /// Enqueue a trigger job for background processing with duplicate prevention
        /// </summary>
        public static void EnqueueTriggerJob(
            string tableName,
            string operationType,
            string recordId,
            string contextJson,
            string transactionId,
            TimeSpan delay = default)
        {
            Random random = new Random();
            int randomNumber = random.Next();
            // Create a unique job key to prevent duplicates
            var jobKey = $"{tableName}:{operationType}:{recordId}:{transactionId}" + randomNumber.ToString();
            
            lock (_jobLock)
            {
                // Check if this exact job has already been processed or is in queue
                if (_processedJobs.Contains(jobKey))
                {
                    _logger?.LogWarning(
                        "[BackgroundTriggerService] Duplicate job detected and skipped: {JobKey}",
                        jobKey);
                    return;
                }

                // Mark job as processed to prevent duplicates
                _processedJobs.Add(jobKey);
            }

            var job = new TriggerJob
            {
                Id = Guid.NewGuid(),
                TableName = tableName,
                OperationType = operationType,
                RecordId = recordId,
                ContextJson = contextJson,
                TransactionId = transactionId,
                ScheduledAt = DateTime.UtcNow.Add(delay),
                CreatedAt = DateTime.UtcNow,
                JobKey = jobKey  // Add job key for tracking
            };

            _jobQueue.Enqueue(job);
            
            _logger?.LogInformation(
                "[BackgroundTriggerService] Enqueued trigger job {JobId} for table {TableName}, operation {OperationType}, scheduled at {ScheduledAt}",
                job.Id, tableName, operationType, job.ScheduledAt);
        }

        /// <summary>
        /// Process queued jobs (called by timer)
        /// </summary>
        private static async void ProcessJobs(object? state)
        {
            if (_isProcessing || _dataProvider == null || _transactionService == null || _logger == null)
                return;

            _isProcessing = true;

            try
            {
                var jobsToProcess = new List<TriggerJob>();
                var now = DateTime.UtcNow;

                // Collect jobs that are ready to process
                while (_jobQueue.TryDequeue(out var job))
                {
                    if (job.ScheduledAt <= now)
                    {
                        jobsToProcess.Add(job);
                    }
                    else
                    {
                        // Re-queue job if not ready yet
                        _jobQueue.Enqueue(job);
                        break; // Stop processing to avoid infinite loop
                    }
                }

                // Process each job
                foreach (var job in jobsToProcess)
                {
                    _ = Task.Run(async () => await ProcessTriggerJob(job));
                }

                // Cleanup old processed jobs (keep only last 1000 to prevent memory issues)
                lock (_jobLock)
                {
                    if (_processedJobs.Count > 1000)
                    {
                        _processedJobs.Clear();
                        _logger?.LogInformation("[BackgroundTriggerService] Cleared processed jobs cache");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[BackgroundTriggerService] Error processing jobs");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Process a single trigger job
        /// </summary>
        private static async Task ProcessTriggerJob(TriggerJob job)
        {
            try
            {
                _logger?.LogInformation(
                    "[BackgroundTriggerService] Processing trigger job {JobId} for table {TableName}",
                    job.Id, job.TableName);

                // Get triggers for this table and operation
                var triggers = await DagTriggerRepository.GetTriggersForTableAsync(job.TableName, job.OperationType);

                if (!triggers.Any())
                {
                    _logger?.LogDebug(
                        "[BackgroundTriggerService] No triggers found for table {TableName}, operation {OperationType}",
                        job.TableName, job.OperationType);
                    return;
                }

                var context = JsonConvert.DeserializeObject<Dictionary<string, object>>(job.ContextJson) 
                    ?? new Dictionary<string, object>();

                foreach (var trigger in triggers)
                {
                    await ProcessSingleTrigger(trigger, job, context);
                }

                _logger?.LogInformation(
                    "[BackgroundTriggerService] Completed processing trigger job {JobId}",
                    job.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[BackgroundTriggerService] Error processing trigger job {JobId} for table {TableName}",
                    job.Id, job.TableName);
            }
        }

        /// <summary>
        /// Process a single trigger configuration
        /// </summary>
        private static async Task ProcessSingleTrigger(DagTriggerConfig trigger, TriggerJob job, Dictionary<string, object> context)
        {
            try
            {
                if (trigger.TriggerType == "DirectInsert")
                {
                    await ProcessDirectInsert(trigger, job, context);
                }
                else // Default to DAGS
                {
                    await ProcessDagTrigger(trigger, job, context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[BackgroundTriggerService] Error processing trigger {TriggerId} of type {TriggerType}",
                    trigger.Id, trigger.TriggerType);
            }
        }

        /// <summary>
        /// Process DirectInsert trigger type - creates child records directly
        /// </summary>
        private static async Task ProcessDirectInsert(DagTriggerConfig trigger, TriggerJob job, Dictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(trigger.InsertConfig))
            {
                _logger?.LogWarning(
                    "[BackgroundTriggerService] DirectInsert trigger {TriggerId} has no InsertConfig",
                    trigger.Id);
                return;
            }

            try
            {
                _logger?.LogInformation(
                    "[BackgroundTriggerService] Processing DirectInsert trigger {TriggerId} for record {RecordId}",
                    trigger.Id, job.RecordId);

                // Parse the InsertConfig JSON
                var insertConfig = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(trigger.InsertConfig);
                
                if (insertConfig == null)
                {
                    _logger?.LogWarning(
                        "[BackgroundTriggerService] Failed to parse InsertConfig for trigger {TriggerId}",
                        trigger.Id);
                    return;
                }

                // Replace |RENGUID| placeholders with actual record ID
                var processedConfig = ReplaceRenGuidPlaceholders(insertConfig, job.RecordId);

                // Create transaction request for child records
                var childRequest = new TransactionRequest
                {
                    TransactionEntityName = "system_generated", // Placeholder
                    TransactionId = $"{job.TransactionId}_child_{trigger.Id}",
                    UserId = context.ContainsKey("userId") ? context["userId"].ToString() : "system",
                    RenProps = processedConfig,
                    UseModelBinding = true // Use model binding for direct inserts
                };

                // Process child records using existing transaction service
                await ProcessChildRecordsFromConfig(childRequest, job.RecordId);

                _logger?.LogInformation(
                    "[BackgroundTriggerService] Successfully processed DirectInsert trigger {TriggerId}",
                    trigger.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[BackgroundTriggerService] Error processing DirectInsert trigger {TriggerId}",
                    trigger.Id);
            }
        }

        /// <summary>
        /// Process DAGS trigger type - calls external DAG service
        /// </summary>
        private static async Task ProcessDagTrigger(DagTriggerConfig trigger, TriggerJob job, Dictionary<string, object> context)
        {
            try
            {
                _logger?.LogInformation(
                    "[BackgroundTriggerService] Processing DAG trigger {TriggerId} with DAG ID {DagId}",
                    trigger.Id, trigger.DagId);

                // Prepare DAG context
                //var dagContext = new Dictionary<string, object>(context)
                //{
                //    { "tableName", job.TableName },
                //    { "recordId", job.RecordId },
                //  //  { "operationType", job.OperationType },
                //    { "transactionId", Guid.NewGuid() }
                //};

                // Create logging callback
                Func<string, string?, string?, int?, int?, Task> onExecutionComplete = async (status, responseData, errorMessage, httpStatusCode, executionTimeMs) =>
                {
                    try
                    {
                        await DagTriggerRepository.LogTriggerExecutionAsync(
                            trigger.Id,
                            trigger.DagId,
                            job.TableName,
                            job.OperationType,
                            job.RecordId,
                            "{}",
                            responseData,
                            status,
                            errorMessage,
                            httpStatusCode,
                            executionTimeMs);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[BackgroundTriggerService] Error logging DAG trigger execution for DAG {DagId}", trigger.DagId);
                    }
                };

                // Trigger DAG asynchronously
                await DagTriggerService.TriggerDagAsync(
                    trigger.DagId,
                    job.TableName,
                    job.RecordId,
                    null,
                    onExecutionComplete);

                _logger?.LogInformation(
                    "[BackgroundTriggerService] Successfully triggered DAG {DagId} for trigger {TriggerId}",
                    trigger.DagId, trigger.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[BackgroundTriggerService] Error processing DAG trigger {TriggerId}",
                    trigger.Id);
            }
        }

        /// <summary>
        /// Replace |RENGUID| placeholders with actual record ID
        /// </summary>
        private static Dictionary<string, List<Dictionary<string, object>>> ReplaceRenGuidPlaceholders(
            Dictionary<string, List<Dictionary<string, object>>> config, 
            string recordId)
        {
            var result = new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (var table in config)
            {
                var processedRecords = new List<Dictionary<string, object>>();

                foreach (var record in table.Value)
                {
                    var processedRecord = new Dictionary<string, object>();

                    foreach (var field in record)
                    {
                        var value = field.Value;
                        
                        if (value?.ToString() == "|RENGUID|")
                        {
                            value = ConvertValueToAppropriateType(field.Key, recordId);
                        }

                        processedRecord[field.Key] = value;
                    }

                    processedRecords.Add(processedRecord);
                }

                result[table.Key] = processedRecords;
            }

            return result;
        }

        /// <summary>
        /// Process child records from configuration using transaction service
        /// </summary>
        private static async Task ProcessChildRecordsFromConfig(TransactionRequest request, string parentRecordId)
        {
            if (_transactionService == null || _dataProvider == null)
            {
                _logger?.LogError("[BackgroundTriggerService] Transaction service or data provider not initialized");
                return;
            }

            try
            {
                using var connection = await _dataProvider.GetConnectionAsync();
                using var transaction = await _dataProvider.BeginTransactionAsync(connection);

                // Process each table in RenProps
                foreach (var tableConfig in request.RenProps)
                {
                    var tableName = tableConfig.Key;
                    var records = tableConfig.Value;

                    _logger?.LogInformation(
                        "[BackgroundTriggerService] Creating {RecordCount} child records in table {TableName}",
                        records.Count, tableName);

                    foreach (var recordData in records)
                    {
                        await CreateChildRecordDirect(tableName, recordData, transaction);
                    }
                }

                transaction.Commit();
                
                _logger?.LogInformation(
                    "[BackgroundTriggerService] Successfully created child records for parent {ParentRecordId}",
                    parentRecordId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[BackgroundTriggerService] Error creating child records for parent {ParentRecordId}",
                    parentRecordId);
                throw;
            }
        }

        /// <summary>
        /// Create a single child record directly with duplicate key handling
        /// </summary>
        private static async Task CreateChildRecordDirect(
            string tableName, 
            Dictionary<string, object> recordData, 
            System.Data.IDbTransaction transaction)
        {
            if (_dataProvider == null)
                return;

            var parameters = new Dictionary<string, object>();
            var uniqueFields = new Dictionary<string, object>(); // Fields that might have unique constraints

            // Build parameters from record data (excluding id for insert)
            foreach (var field in recordData)
            {
                if (field.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                    continue; // Skip id for insert

                if (field.Value != null)
                {
                    var convertedValue = ConvertValueToAppropriateType(field.Key, field.Value);
                    parameters[field.Key.ToLower()] = convertedValue;
                    
                    // Track potential unique fields for duplicate checking
                    var lowerKey = field.Key.ToLower();
                    if (lowerKey.Contains("id") || lowerKey.Contains("code") || lowerKey.Contains("number") || 
                        lowerKey.Contains("email") || lowerKey.Contains("username") || lowerKey.Contains("caseid") ||
                        lowerKey.Contains("userid") || lowerKey.Contains("partyroleid"))
                    {
                        uniqueFields[field.Key.ToLower()] = convertedValue;
                    }
                }
            }

            if (parameters.Any())
            {
                try
                {
                    // Try to insert the record
                    await _dataProvider.ExecuteInsertWithReturnAsync(tableName, parameters, "id", transaction);
                    
                    _logger?.LogDebug(
                        "[BackgroundTriggerService] Created child record in table {TableName}",
                        tableName);
                }
                catch (Exception ex)
                {
                    // Check if it's a duplicate key error
                    var errorMessage = ex.Message.ToLower();
                    if (errorMessage.Contains("duplicate") || errorMessage.Contains("unique") || 
                        errorMessage.Contains("constraint") || errorMessage.Contains("violation") ||
                        errorMessage.Contains("already exists"))
                    {
                        _logger?.LogWarning(
                            "[BackgroundTriggerService] Duplicate key detected for table {TableName}: {Error}",
                            tableName, ex.Message);

                        // Try to find existing record using unique fields
                        if (uniqueFields.Any())
                        {
                            var existingRecord = await CheckIfRecordExistsAsync(tableName, uniqueFields, transaction);
                            if (existingRecord)
                            {
                                _logger?.LogInformation(
                                    "[BackgroundTriggerService] Record already exists in table {TableName}, skipping insert",
                                    tableName);
                                return; // Record exists, skip insert
                            }
                        }

                        // If we can't determine if record exists, log the error but don't fail
                        _logger?.LogError(ex,
                            "[BackgroundTriggerService] Failed to create child record in table {TableName} due to duplicate key",
                            tableName);
                    }
                    else
                    {
                        // Re-throw non-duplicate key errors
                        _logger?.LogError(ex,
                            "[BackgroundTriggerService] Error creating child record in table {TableName}",
                            tableName);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Check if a record already exists based on unique fields
        /// </summary>
        private static async Task<bool> CheckIfRecordExistsAsync(
            string tableName, 
            Dictionary<string, object> uniqueFields, 
            System.Data.IDbTransaction transaction)
        {
            try
            {
                if (_dataProvider == null || !uniqueFields.Any())
                    return false;

                // Build a simple SELECT query to check existence
                var formattedTableName = _dataProvider.FormatTableName(tableName);
                var whereConditions = uniqueFields.Select(f => $"{f.Key} = {_dataProvider.GetParameterPlaceholder(f.Key)}").ToList();
                var sql = $"SELECT COUNT(*) FROM {formattedTableName} WHERE {string.Join(" AND ", whereConditions)}";

                var result = await _dataProvider.ExecuteScalarAsync(sql, uniqueFields, transaction);
                var count = Convert.ToInt32(result);

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "[BackgroundTriggerService] Error checking if record exists in table {TableName}",
                    tableName);
                return false; // Assume doesn't exist if we can't check
            }
        }

        /// <summary>
        /// Check if a field name is likely to be a UUID field
        /// </summary>
        private static bool IsLikelyUuidField(string fieldName)
        {
            var lowerFieldName = fieldName.ToLower();
            return lowerFieldName.Contains("id") || 
                   lowerFieldName.Contains("guid") || 
                   lowerFieldName.EndsWith("_id") ||
                   lowerFieldName.StartsWith("id_") ||
                   lowerFieldName == "recordid" ||
                   lowerFieldName.Contains("caseid") ||
                   lowerFieldName.Contains("userid") ||
                   lowerFieldName.Contains("partyroleid");
        }

        /// <summary>
        /// Convert value to appropriate type based on field name and content
        /// </summary>
        private static object ConvertValueToAppropriateType(string fieldName, object value)
        {
            if (value == null) return DBNull.Value;

            var stringValue = value.ToString();
            
            // Handle special placeholders first
            if (stringValue == "|Todaydate|")
            {
                return DateTime.Now;
            }

            // Try UUID conversion for likely UUID fields
            if (IsLikelyUuidField(fieldName) && Guid.TryParse(stringValue, out var guidValue))
            {
                return guidValue;
            }

            // Try integer conversion for numeric-looking values
            if (int.TryParse(stringValue, out var intValue))
            {
                return intValue;
            }

            // Try decimal conversion
            if (decimal.TryParse(stringValue, out var decimalValue))
            {
                return decimalValue;
            }

            // Try boolean conversion
            if (bool.TryParse(stringValue, out var boolValue))
            {
                return boolValue;
            }

            // Try DateTime conversion
            if (DateTime.TryParse(stringValue, out var dateValue))
            {
                return dateValue;
            }

            // Default to string
            return value;
        }

        /// <summary>
        /// Get queue statistics
        /// </summary>
        public static (int QueuedJobs, bool IsProcessing) GetStatus()
        {
            return (_jobQueue.Count, _isProcessing);
        }
    }

    /// <summary>
    /// Represents a background trigger job
    /// </summary>
    public class TriggerJob
    {
        public Guid Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string ContextJson { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string JobKey { get; set; } = string.Empty; // Unique key for duplicate prevention
    }
}