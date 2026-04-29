using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace KATCRUDServices.Core.Services
{
    public partial class TransactionService : ITransactionService
    {
        public async Task<BulkTransactionResult> ProcessBulkTransactionAsync(BulkTransactionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var batchId = string.IsNullOrEmpty(request.BatchId) ? Guid.NewGuid().ToString() : request.BatchId;

            _logger.LogInformation("Starting bulk transaction {BatchId} with {RecordCount} records", 
                batchId, request.Transactions.Count);

            var result = new BulkTransactionResult
            {
                BatchId = batchId,
                TotalRecords = request.Transactions.Count,
                SuccessfulRecords = 0,
                FailedRecords = 0
            };

            if (!request.Transactions.Any())
            {
                result.Success = true;
                result.Message = "No transactions to process";
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Group transactions by entity name for bulk processing
            var transactionsByEntity = request.Transactions
                .Select((t, index) => (Transaction: t, Index: index))
                .GroupBy(x => x.Transaction.TransactionEntityName);

            foreach (var entityGroup in transactionsByEntity)
            {
                var entityName = entityGroup.Key;
                var entityTransactions = entityGroup.ToList();

                _logger.LogInformation("Processing {Count} transactions for entity {EntityName}", 
                    entityTransactions.Count, entityName);

                try
                {
                    // Get field mappers once for the entity
                    var mappers = await _fieldMapperService.GetFieldMappersAsync(entityName);
                    
                    if (!mappers.Any())
                    {
                        _logger.LogError("No field mappers found for entity: {EntityName}", entityName);
                        
                        foreach (var item in entityTransactions)
                        {
                            result.FailedResults.Add(new BulkTransactionError
                            {
                                TransactionId = item.Transaction.TransactionId,
                                RecordIndex = item.Index,
                                ErrorMessage = $"No field mappers found for entity: {entityName}"
                            });
                            result.FailedRecords++;
                        }
                        continue;
                    }

                    // Process based on operation type
                    await ProcessBulkEntityTransactionsAsync(
                        entityName, 
                        entityTransactions, 
                        mappers, 
                        result, 
                        request.ContinueOnError);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bulk transactions for entity {EntityName}", entityName);
                    
                    if (!request.ContinueOnError)
                    {
                        result.Success = false;
                        result.Message = $"Bulk transaction failed for entity {entityName}: {ex.Message}";
                        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                        return result;
                    }

                    foreach (var item in entityTransactions)
                    {
                        result.FailedResults.Add(new BulkTransactionError
                        {
                            TransactionId = item.Transaction.TransactionId,
                            RecordIndex = item.Index,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                        result.FailedRecords++;
                    }
                }
            }

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Success = result.FailedRecords == 0;
            result.Message = result.Success 
                ? $"All {result.SuccessfulRecords} records processed successfully" 
                : $"{result.SuccessfulRecords} succeeded, {result.FailedRecords} failed";

            _logger.LogInformation("Bulk transaction {BatchId} completed: {Message} in {ElapsedMs}ms", 
                batchId, result.Message, result.ExecutionTimeMs);

            return result;
        }

        private async Task ProcessBulkEntityTransactionsAsync(
            string entityName,
            List<(TransactionRequest Transaction, int Index)> entityTransactions,
            List<FieldMapper> mappers,
            BulkTransactionResult result,
            bool continueOnError)
        {
            IDbConnection? connection = null;
            IDbTransaction? transaction = null;

            try
            {
                connection = await _transactionDataProvider.GetConnectionAsync();
                transaction = await _transactionDataProvider.BeginTransactionAsync(connection);

                // Separate inserts, updates, and records with child data
                var insertsWithIndex = new List<(TransactionRequest request, int index)>();
                var updatesWithIndex = new List<(TransactionRequest request, int index)>();
                var insertsWithChildrenWithIndex = new List<(TransactionRequest request, int index)>();
                Dictionary<string, List<TriggerEntry>> _trggersCache = new();
                var auditEnabledCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                var mappersCache = new Dictionary<string, List<FieldMapper>>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in entityTransactions)
                {
                    var txRequest = item.Transaction;
                    var index = item.Index;

                    // Validate if validation service is available
                    if (_validationService != null)
                    {
                        var validationResult = await _validationService.ValidateAsync(
                            txRequest.TransactionEntityName, 
                            txRequest.ExtendedProperties, 
                            transaction);
                        
                        if (!validationResult.IsValid)
                        {
                            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                            result.FailedResults.Add(new BulkTransactionError
                            {
                                TransactionId = txRequest.TransactionId,
                                RecordIndex = index,
                                ErrorMessage = $"Validation failed: {errorMessages}",
                                Data = new Dictionary<string, object>
                                {
                                    { "ValidationErrors", validationResult.Errors }
                                }
                            });
                            result.FailedRecords++;
                            
                            if (!continueOnError)
                            {
                                throw new InvalidOperationException($"Validation failed for record {index}: {errorMessages}");
                            }
                            continue;
                        }
                    }

                    // Determine if insert or update
                    var isUpdate = DetermineOperationType(txRequest, mappers) == "Update";
                    
                    if (isUpdate)
                    {
                        updatesWithIndex.Add((txRequest, index));
                    }
                    else
                    {
                        // Check if record has child data (RenProps or DelProps)
                        var hasChildData = (txRequest.RenProps != null && txRequest.RenProps.Any()) || 
                                          (txRequest.DelProps != null && txRequest.DelProps.Any());
                        
                        if (hasChildData)
                        {
                            // Process individually to get RENGUID for child records
                            insertsWithChildrenWithIndex.Add((txRequest, index));
                        }
                        else
                        {
                            // Can use bulk insert
                            insertsWithIndex.Add((txRequest, index));
                        }
                    }
                }

                // Process bulk inserts (records without child data)
                if (insertsWithIndex.Any())
                {
                    await ProcessBulkInsertsAsync(
                        entityName, 
                        insertsWithIndex, 
                        mappers, 
                        transaction, 
                        result, 
                        continueOnError);
                }

                // Process inserts with child records individually
                foreach (var (insertRequest, index) in insertsWithChildrenWithIndex)
                {
                    try
                    {
                        var transactionId = string.IsNullOrEmpty(insertRequest.TransactionId) 
                            ? Guid.NewGuid().ToString() 
                            : insertRequest.TransactionId;

                        var parentdetails = await CreateMainRecordAsync(insertRequest, mappers, transaction, transactionId,false);
                        var renGuid = parentdetails.RenGuid;

                        // Process child records
                        await ProcessChildRecordsAsync(insertRequest, renGuid, transaction, transactionId, _trggersCache, false, auditEnabledCache, mappersCache);
                        //await ProcessChildRecordsAsync(insertRequest, renGuid, connection, transaction, transactionId, _trggersCache);
                        
                        // Process deletes
                        await ProcessDeleteRecordsAsync(insertRequest, connection, transaction, transactionId);

                        result.SuccessResults.Add(new TransactionResult
                        {
                            Success = true,
                            TransactionId = transactionId,
                            Message = "Insert with child records completed successfully",
                            Data = new Dictionary<string, object> { { "RENGUID", renGuid } }
                        });
                        result.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing insert with child records for record {Index}", index);
                        
                        result.FailedResults.Add(new BulkTransactionError
                        {
                            TransactionId = insertRequest.TransactionId,
                            RecordIndex = index,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                        result.FailedRecords++;

                        if (!continueOnError)
                        {
                            throw;
                        }
                    }
                }

                // Process updates individually (updates are typically not bulk-optimized)
                foreach (var (updateRequest, index) in updatesWithIndex)
                {
                    try
                    {
                        var transactionId = string.IsNullOrEmpty(updateRequest.TransactionId) 
                            ? Guid.NewGuid().ToString() 
                            : updateRequest.TransactionId;

                        var renGuid = await CreateMainRecordAsync(updateRequest, mappers, transaction, transactionId,false);

                        // Process child records
                        await ProcessChildRecordsAsync(updateRequest, renGuid, transaction, transactionId, _trggersCache, false, auditEnabledCache, mappersCache);
                        //await ProcessChildRecordsAsync(updateRequest, renGuid, connection, transaction, transactionId, _trggersCache);
                        
                        // Process deletes
                        await ProcessDeleteRecordsAsync(updateRequest, connection, transaction, transactionId);

                        result.SuccessResults.Add(new TransactionResult
                        {
                            Success = true,
                            TransactionId = transactionId,
                            Message = "Update completed successfully",
                            Data = new Dictionary<string, object> { { "RENGUID", renGuid } }
                        });
                        result.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing update for record {Index}", index);
                        
                        result.FailedResults.Add(new BulkTransactionError
                        {
                            TransactionId = updateRequest.TransactionId,
                            RecordIndex = index,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                        result.FailedRecords++;

                        if (!continueOnError)
                        {
                            throw;
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk entity transaction processing for {EntityName}", entityName);
                
                try
                {
                    transaction?.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback bulk transaction");
                }

                throw;
            }
            finally
            {
                transaction?.Dispose();
                connection?.Close();
                connection?.Dispose();
            }
        }

        private async Task ProcessBulkInsertsAsync(
            string entityName,
            List<(TransactionRequest request, int index)> inserts,
            List<FieldMapper> mappers,
            IDbTransaction transaction,
            BulkTransactionResult result,
            bool continueOnError)
        {
            _logger.LogInformation("Processing {Count} bulk inserts for {EntityName}", inserts.Count, entityName);

            // Build DataTable for bulk insert
            var dataTable = new DataTable(entityName);
            var idColumnName = "";
            var idDataType = "";

            // Add columns (excluding AutoGenerated ID)
            foreach (var mapper in mappers.Where(m => m.IsActive))
            {
                if (mapper.Properties.Contains("AutoGenerated"))
                {
                    idColumnName = mapper.ColumnName;
                    idDataType = mapper.DataType.ToLower();
                    continue; // Skip ID column for bulk insert
                }

                var columnType = GetSystemType(mapper.DataType);
                dataTable.Columns.Add(mapper.ColumnName, columnType);
            }

            // Populate rows
            var rowIndexMap = new Dictionary<int, (TransactionRequest request, int originalIndex)>();
            int rowIndex = 0;

            foreach (var (request, originalIndex) in inserts)
            {
                try
                {
                    var row = dataTable.NewRow();
                    
                    foreach (var mapper in mappers.Where(m => m.IsActive && !m.Properties.Contains("AutoGenerated")))
                    {
                        var transactionId = string.IsNullOrEmpty(request.TransactionId) 
                            ? Guid.NewGuid().ToString() 
                            : request.TransactionId;

                        var value = await GetFieldValue(request.ExtendedProperties, mapper, null, transactionId);
                        
                        if (value != null)
                        {
                            row[mapper.ColumnName] = value;
                        }
                        else
                        {
                            row[mapper.ColumnName] = DBNull.Value;
                        }
                    }

                    dataTable.Rows.Add(row);
                    rowIndexMap[rowIndex] = (request, originalIndex);
                    rowIndex++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error preparing bulk insert row for record {Index}", originalIndex);
                    
                    result.FailedResults.Add(new BulkTransactionError
                    {
                        TransactionId = request.TransactionId,
                        RecordIndex = originalIndex,
                        ErrorMessage = $"Error preparing row: {ex.Message}",
                        Exception = ex
                    });
                    result.FailedRecords++;

                    if (!continueOnError)
                    {
                        throw;
                    }
                }
            }

            if (dataTable.Rows.Count == 0)
            {
                _logger.LogWarning("No valid rows to bulk insert for {EntityName}", entityName);
                return;
            }

            // Execute bulk insert
            try
            {
                await _transactionDataProvider.BulkInsertAsync(entityName, dataTable, transaction);

                // Mark all as successful
                foreach (var kvp in rowIndexMap)
                {
                    var (request, originalIndex) = kvp.Value;
                    var transactionId = string.IsNullOrEmpty(request.TransactionId) 
                        ? Guid.NewGuid().ToString() 
                        : request.TransactionId;

                    result.SuccessResults.Add(new TransactionResult
                    {
                        Success = true,
                        TransactionId = transactionId,
                        Message = "Bulk insert completed successfully",
                        Data = new Dictionary<string, object> { { "EntityName", entityName } }
                    });
                    result.SuccessfulRecords++;
                }

                _logger.LogInformation("Bulk insert completed for {EntityName}: {RowCount} rows", entityName, dataTable.Rows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert failed for {EntityName}", entityName);

                // Mark all as failed
                foreach (var kvp in rowIndexMap)
                {
                    var (request, originalIndex) = kvp.Value;
                    
                    result.FailedResults.Add(new BulkTransactionError
                    {
                        TransactionId = request.TransactionId,
                        RecordIndex = originalIndex,
                        ErrorMessage = $"Bulk insert failed: {ex.Message}",
                        Exception = ex
                    });
                    result.FailedRecords++;
                }

                if (!continueOnError)
                {
                    throw;
                }
            }
        }

        private Type GetSystemType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "int" or "integer" => typeof(int),
                "bigint" or "long" => typeof(long),
                "smallint" or "short" => typeof(short),
                "decimal" or "numeric" or "money" => typeof(decimal),
                "float" or "double" or "real" => typeof(double),
                "bool" or "boolean" or "bit" => typeof(bool),
                "date" or "datetime" or "timestamp" => typeof(DateTime),
                "guid" or "uuid" or "uniqueidentifier" => typeof(Guid),
                "string" or "varchar" or "nvarchar" or "text" or "char" or "nchar" => typeof(string),
                "json" or "jsonb" => typeof(string),
                _ => typeof(string)
            };
        }
    }
}
