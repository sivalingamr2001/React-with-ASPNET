# Audit Logging Integration Guide

## Overview

This guide shows how to integrate audit logging into the TransactionService for tracking CREATE, UPDATE, and DELETE operations.

## Setup

### 1. Database Setup

Run the SQL script to create the audit table:

```bash
psql -U your_user -d your_database -f Database/Setup_AuditLog_Tables.sql
```

### 2. Enable Audit for Tables

```sql
-- Enable audit for specific tables
UPDATE public.applicationtable
SET enableaudit = true
WHERE tablename IN ('users', 'orders', 'leaverequest');

-- Verify configuration
SELECT tablename, enableaudit
FROM public.applicationtable
WHERE enableaudit = true;
```

### 3. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using KATCRUDServices.Core.Extensions;

// Add audit logging services
services.AddAuditLogging(queueCapacity: 10000);
```

This registers:
- `IAuditQueue` (Singleton)
- `IAuditService` (Scoped)
- `AuditBackgroundService` (HostedService)
- `AuditWriter`, `AuditDiffBuilder`, `ApplicationTableMetadataRepository` (Scoped)

## Integration in TransactionService

### Step 1: Inject IAuditService

Add `IAuditService` to the TransactionService constructor:

```csharp
public partial class TransactionService : ITransactionService
{
    private readonly IEnhancedDataProvider _transactionDataProvider;
    private readonly IEnhancedConnectionFactory _connectionFactory;
    private readonly DatabaseConfig _databaseConfig;
    private readonly FieldMapperService _fieldMapperService;
    private readonly DataTypeConverter _dataTypeConverter;
    private readonly IValidationService? _validationService;
    private readonly IAuditService _auditService; // ADD THIS
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
        IAuditService auditService = null) // ADD THIS
    {
        _transactionDataProvider = transactionDataProvider;
        _connectionFactory = connectionFactory;
        _databaseConfig = databaseConfig;
        _fieldMapperService = fieldMapperService;
        _dataTypeConverter = dataTypeConverter;
        _validationService = validationService;
        _auditService = auditService; // ADD THIS
        _logger = logger;
        _configuration = configuration;

        // ... rest of constructor
    }
}
```

### Step 2: Add Audit Calls in CREATE Operations

In `CreateMainRecordModelBindingAsync` method (after INSERT):

```csharp
private async Task<object> CreateMainRecordModelBindingAsync(
    TransactionRequest request,
    IDbConnection connection,
    IDbTransaction transaction,
    string transactionId)
{
    _logger.LogInformation("Creating main record using model binding for entity {EntityName}", request.TransactionEntityName);

    try
    {
        var operationType = DetermineOperationTypeModelBinding(request);
        
        if (operationType == "Update")
        {
            return await UpdateMainRecordModelBindingAsync(request, connection, transaction, transactionId);
        }

        // INSERT operation
        var tableName = request.TransactionEntityName;
        var properties = request.ExtendedProperties ?? new Dictionary<string, object>();
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new Dictionary<string, object>();
        var renGuid = Guid.NewGuid();

        // ... build INSERT statement ...

        var sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
        
        _logger.LogDebug("Executing INSERT SQL: {Sql}", sql);
        
        await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);
        
        _logger.LogInformation("Successfully created main record with ID {RenGuid}", renGuid);

        // AUDIT: Fire-and-forget audit for CREATE operation
        if (_auditService != null)
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

        return renGuid;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create main record for entity {EntityName}", request.TransactionEntityName);
        throw;
    }
}
```

### Step 3: Add Audit Calls in UPDATE Operations

In `UpdateMainRecordModelBindingAsync` method (BEFORE UPDATE):

```csharp
private async Task<object> UpdateMainRecordModelBindingAsync(
    TransactionRequest request,
    IDbConnection connection,
    IDbTransaction transaction,
    string transactionId)
{
    _logger.LogInformation("Updating main record using model binding for entity {EntityName}", request.TransactionEntityName);

    try
    {
        var tableName = request.TransactionEntityName;
        var properties = request.ExtendedProperties ?? new Dictionary<string, object>();

        if (!properties.TryGetValue("id", out var idValue) || idValue == null)
        {
            throw new InvalidOperationException("ID is required for update operations");
        }

        var recordId = Guid.Parse(idValue.ToString());
        var modifiedBy = properties.ContainsKey("userid") 
            ? properties["userid"]?.ToString() ?? "system" 
            : "system";

        // AUDIT: Call BEFORE update to capture existing data
        if (_auditService != null)
        {
            await _auditService.TryAuditAsync(
                tableName,
                recordId,
                AuditOperation.UPDATE,
                modifiedBy,
                properties,
                connection,
                transaction);
        }

        var setClauses = new List<string>();
        var parameters = new Dictionary<string, object>();
        parameters["id"] = idValue;

        // ... build UPDATE statement ...

        var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE id = @id";
        
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
```

### Step 4: Add Audit Calls in DELETE Operations

In `ProcessDeleteRecordsAsync` method (BEFORE DELETE):

```csharp
private async Task ProcessDeleteRecordsAsync(
    TransactionRequest request,
    IDbConnection connection,
    IDbTransaction transaction,
    string transactionId)
{
    if (request.DeleteRecords == null || !request.DeleteRecords.Any())
    {
        return;
    }

    _logger.LogInformation("Processing {Count} delete operations for transaction {TransactionId}", 
        request.DeleteRecords.Count, transactionId);

    foreach (var deleteRecord in request.DeleteRecords)
    {
        try
        {
            var tableName = deleteRecord.TableName;
            var recordId = Guid.Parse(deleteRecord.RecordId);
            var modifiedBy = request.ExtendedProperties?.ContainsKey("userid") == true
                ? request.ExtendedProperties["userid"]?.ToString() ?? "system"
                : "system";

            // AUDIT: Call BEFORE delete to capture existing data
            if (_auditService != null)
            {
                await _auditService.TryAuditAsync(
                    tableName,
                    recordId,
                    AuditOperation.DELETE,
                    modifiedBy,
                    null, // No extended properties for delete
                    connection,
                    transaction);
            }

            // Perform the delete
            var sql = $"DELETE FROM {tableName} WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "id", recordId } };
            
            await _transactionDataProvider.ExecuteNonQueryAsync(sql, parameters, connection, transaction);
            
            _logger.LogInformation("Deleted record {RecordId} from {TableName}", recordId, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete record from {TableName}", deleteRecord.TableName);
            throw;
        }
    }
}
```

### Step 5: Add Audit for Child Tables

In `ProcessChildRecordsModelBindingAsync` method:

```csharp
private async Task ProcessChildRecordsModelBindingAsync(
    TransactionRequest request,
    object renGuid,
    IDbConnection connection,
    IDbTransaction transaction,
    string transactionId,
    Dictionary<string, List<TriggerEntry>> triggersCache)
{
    if (request.ChildRecords == null || !request.ChildRecords.Any())
    {
        return;
    }

    foreach (var childRecord in request.ChildRecords)
    {
        var childTableName = childRecord.TableName;
        var childProperties = childRecord.ExtendedProperties ?? new Dictionary<string, object>();
        
        // Determine if this is an insert or update
        var isUpdate = childProperties.TryGetValue("id", out var childIdValue) && 
                       childIdValue != null && 
                       !string.IsNullOrWhiteSpace(childIdValue.ToString()) &&
                       childIdValue.ToString() != "00000000-0000-0000-0000-000000000000";

        Guid childRecordId;
        var modifiedBy = request.ExtendedProperties?.ContainsKey("userid") == true
            ? request.ExtendedProperties["userid"]?.ToString() ?? "system"
            : "system";

        if (isUpdate)
        {
            childRecordId = Guid.Parse(childIdValue.ToString());

            // AUDIT: Call BEFORE update for child record
            if (_auditService != null)
            {
                await _auditService.TryAuditAsync(
                    childTableName,
                    childRecordId,
                    AuditOperation.UPDATE,
                    modifiedBy,
                    childProperties,
                    connection,
                    transaction);
            }

            // Perform UPDATE
            // ... your update logic ...
        }
        else
        {
            childRecordId = Guid.NewGuid();
            
            // Perform INSERT
            // ... your insert logic ...

            // AUDIT: Fire-and-forget audit for CREATE child record
            if (_auditService != null)
            {
                _ = _auditService.TryAuditAsync(
                    childTableName,
                    childRecordId,
                    AuditOperation.CREATE,
                    modifiedBy,
                    childProperties,
                    connection,
                    transaction);
            }
        }
    }
}
```

## Important Rules

### ✅ DO

1. **Call audit BEFORE update/delete** - This captures existing data within the transaction
2. **Pass the transaction** - For UPDATE/DELETE, pass the transaction so audit can read existing data
3. **Use fire-and-forget for CREATE** - After successful insert, use `_ = _auditService.TryAuditAsync(...)`
4. **Check for null** - Always check if `_auditService != null` before calling
5. **Get modifiedBy from request** - Extract user ID from `request.ExtendedProperties["userid"]`

### ❌ DON'T

1. **Don't await audit calls after commit** - Defeats the fire-and-forget pattern
2. **Don't throw exceptions from audit** - Audit failures should not impact main transaction
3. **Don't call audit AFTER update/delete** - Existing data will be lost
4. **Don't block on audit writes** - The background service handles writes asynchronously

## Querying Audit Logs

```sql
-- Get audit trail for a specific record
SELECT 
    transactiontable,
    transactionid,
    modifiedby,
    operation,
    changelog,
    createdon
FROM public.auditlog
WHERE transactionid = 'your-guid-here'
ORDER BY createdon DESC;

-- Get all changes by a user
SELECT * FROM public.auditlog
WHERE modifiedby = 'user@example.com'
ORDER BY createdon DESC;

-- Extract specific field changes
SELECT 
    transactionid,
    modifiedby,
    changelog->'changes'->'email'->>'before' as old_email,
    changelog->'changes'->'email'->>'after' as new_email,
    createdon
FROM public.auditlog
WHERE transactiontable = 'users'
  AND changelog->'changes' ? 'email'
ORDER BY createdon DESC;
```

## Changelog Format

```json
{
  "changeUser": "user@example.com",
  "timestamp": "2024-01-15T10:30:00Z",
  "table": "users",
  "transactionId": "123e4567-e89b-12d3-a456-426614174000",
  "operation": "UPDATE",
  "changes": {
    "email": {
      "before": "old@example.com",
      "after": "new@example.com"
    },
    "status": {
      "before": "active",
      "after": "inactive"
    }
  }
}
```

## Monitoring

Check logs for audit service activity:

```
✅ "Audit Background Service started"
✅ "Audit record written for users/123e4567..."
⚠️  "Audit queue is full. Dropping audit job"
❌ "Failed to write audit record for orders/456f7890..."
```

## Performance

- **Non-blocking**: Main transactions never wait for audit writes
- **Fire-and-forget**: Audit jobs are queued and processed in background
- **Bounded queue**: Prevents memory exhaustion (default 10,000 jobs)
- **Independent connections**: Audit writes use separate DB connections

## Troubleshooting

### Audit records not being created

1. Check if audit is enabled:
   ```sql
   SELECT tablename, enableaudit FROM public.applicationtable WHERE tablename = 'your_table';
   ```

2. Check background service logs:
   ```
   Look for: "Audit Background Service started"
   ```

3. Check for queue full warnings:
   ```
   Look for: "Audit queue is full. Dropping audit job"
   ```

### Existing data not captured for UPDATE

Ensure you call `TryAuditAsync` BEFORE the update and pass the transaction:

```csharp
// CORRECT
await _auditService.TryAuditAsync(..., connection, transaction); // Before update
await PerformUpdate();

// WRONG
await PerformUpdate(); // Update first
await _auditService.TryAuditAsync(..., connection, null); // Too late!
```

## Summary

The audit logging implementation provides production-ready tracking of all data changes with:
- Fire-and-forget pattern for zero performance impact
- Automatic capture of before/after values
- Background processing with graceful failure handling
- Per-table configuration via `enableaudit` field
- Complete audit trail in JSONB format
