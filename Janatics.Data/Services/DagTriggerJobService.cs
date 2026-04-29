using Hangfire;
using KATCRUDServices.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Service for handling DAG triggers as Hangfire background jobs
    /// </summary>
    public class DagTriggerJobService
    {
        /// <summary>
        /// Process DAG triggers after transaction commit
        /// This method is called by Hangfire as a background job
        /// </summary>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
        public static async Task ProcessDagTriggersAsync(
            string tableName,
            string operationType,
            string recordId,
            string extendedPropertiesJson,
            string transactionId,
            string triggersJson)
        {
            Console.WriteLine($"[DagTriggerJobService] Processing DAG triggers for table {tableName}, operation {operationType}, record {recordId}");

            //await Task.Delay(2000);
            try
            {
                // Deserialize extended properties
                var extendedProperties = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(extendedPropertiesJson) 
                    ?? new Dictionary<string, object>();

                // Deserialize cached triggers
                var triggersCache = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<TriggerEntryDto>>>(triggersJson) 
                    ?? new Dictionary<string, List<TriggerEntryDto>>();

                // Get triggers for this table and operation
                var triggers = await DagTriggerRepository.GetTriggersForTableAsync(tableName, operationType);

                if (triggers.Any())
                {
                    Console.WriteLine($"[DagTriggerJobService] Found {triggers.Count} DAG triggers for table {tableName}, operation {operationType}");

                    // Prepare context data for DAG
                    var dagContext = new Dictionary<string, object>(extendedProperties)
                    {
                        { "operationType", operationType },
                        { "transactionId", transactionId }
                    };

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
                                    tableName,
                                    operationType,
                                    recordId,
                                    Newtonsoft.Json.JsonConvert.SerializeObject(dagContext),
                                    responseData,
                                    status,
                                    errorMessage,
                                    httpStatusCode,
                                    executionTimeMs);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DagTriggerJobService] Error logging DAG trigger execution for DAG {trigger.DagId}: {ex.Message}");
                            }
                        };

                        _ = DagTriggerService.TriggerDagAsync(
                            trigger.DagId,
                            tableName,
                            recordId,
                            dagContext,
                            onExecutionComplete);
                    }
                }

                // Process cached child record triggers
                if (triggersCache.Any())
                {
                    Console.WriteLine($"[DagTriggerJobService] Processing {triggersCache.Sum(x => x.Value.Count)} cached child triggers");

                    foreach (var tableTriggers in triggersCache)
                    {
                        foreach (var triggerEntry in tableTriggers.Value)
                        {
                            // Create logging callback for child triggers
                            Func<string, string?, string?, int?, int?, Task> onChildExecutionComplete = async (status, responseData, errorMessage, httpStatusCode, executionTimeMs) =>
                            {
                                try
                                {
                                    await DagTriggerRepository.LogTriggerExecutionAsync(
                                        Guid.Parse(triggerEntry.TriggerId),
                                        Guid.Parse(triggerEntry.DagId),
                                        triggerEntry.ChildTableName,
                                        triggerEntry.OperationType,
                                        triggerEntry.ChildRecordId,
                                        triggerEntry.ChildDagContextJson,
                                        responseData,
                                        status,
                                        errorMessage,
                                        httpStatusCode,
                                        executionTimeMs);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DagTriggerJobService] Error logging child DAG trigger execution: {ex.Message}");
                                }
                            };

                            var childDagContext = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(triggerEntry.ChildDagContextJson) 
                                ?? new Dictionary<string, object>();

                            _ = DagTriggerService.TriggerDagAsync(
                                Guid.Parse(triggerEntry.DagId),
                                triggerEntry.ChildTableName,
                                triggerEntry.ChildRecordId,
                                childDagContext,
                                onChildExecutionComplete);
                        }
                    }
                }

                Console.WriteLine($"[DagTriggerJobService] Successfully processed DAG triggers for transaction {transactionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DagTriggerJobService] Error processing DAG triggers for transaction {transactionId}: {ex.Message}");
                // Re-throw to trigger Hangfire retry
                throw;
            }
        }
    }

    /// <summary>
    /// DTO for serializing trigger entries to pass to Hangfire
    /// </summary>
    public class TriggerEntryDto
    {
        public string TriggerId { get; set; } = string.Empty;
        public string DagId { get; set; } = string.Empty;
        public string ChildTableName { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string ChildRecordId { get; set; } = string.Empty;
        public string ChildDagContextJson { get; set; } = string.Empty;
    }
}
