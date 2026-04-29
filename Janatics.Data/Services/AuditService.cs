using System.Data;
using System.Text.RegularExpressions;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Service for coordinating audit operations.
    /// All auditlog processing (building changelog and writing to the auditlog table) runs only after transaction commit,
    /// in FlushAuditLogsFireAndForget on a new connection. During the transaction we only enqueue jobs (and for UPDATE/DELETE we may read current row once to capture before state).
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditQueue _auditQueue;
        private readonly AuditDiffBuilder _diffBuilder;
        private readonly ILogger<AuditService> _logger;

        /// <summary>
        /// Per-async-context cache of audit jobs to be written after transaction commit (like triggers cache).
        /// When a transaction is active, audit jobs are added here; after commit, FlushAuditLogsFireAndForget writes them on a new connection.
        /// </summary>
        private static readonly AsyncLocal<List<AuditJob>?> _auditLogCache = new();
        private static readonly AsyncLocal<Dictionary<string, string?>?> _tableDisplayNameCache = new();

        private static readonly Regex TableNameRegex = new(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);

        public AuditService(
            IAuditQueue auditQueue,
            AuditDiffBuilder diffBuilder,
            ILogger<AuditService> logger)
        {
            _auditQueue = auditQueue;
            _diffBuilder = diffBuilder;
            _logger = logger;
        }

        public async Task<bool> TryAuditAsync(
            string entityName,
            Guid transactionId,
            AuditOperation operation,
            string modifiedBy,
            Dictionary<string, object?>? extendedProperties,
            IDbConnection connection,
            IDbTransaction? transaction = null,
            Dictionary<string, object?>? beforeData = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // During transaction we only enqueue; no write to auditlog until after commit (FlushAuditLogsFireAndForget).
                // For UPDATE/DELETE we need before state: use caller-provided beforeData when present to avoid any read during the transaction.
                Dictionary<string, object?>? before = beforeData;
                if (before == null && (operation == AuditOperation.UPDATE || operation == AuditOperation.DELETE))
                {
                    before = await ReadExistingDataAsync(
                        entityName,
                        transactionId,
                        connection,
                        transaction,
                        cancellationToken).ConfigureAwait(false);

                    if (before == null)
                    {
                        _logger.LogWarning(
                            "Could not read existing data for {Table}/{Id}. Audit will be skipped.",
                            entityName,
                            transactionId);
                        return false;
                    }
                }

                // Normalize special placeholder values in extended properties (e.g., "now", "|RENGUID|")
                var normalizedExtended = NormalizeExtendedProperties(extendedProperties, transactionId);

                // Do NOT run applicationtable or portaluser queries here - connection may have an open reader (NpgsqlOperationInProgressException).
                // Table display name and change-user display name are resolved later when writing on a new connection (WriteAuditOnNewConnectionAsync).
                var auditJob = new AuditJob
                {
                    EntityName = entityName,
                    TransactionTable = entityName,
                    TableDisplayName = null,
                    TransactionId = transactionId,
                    Operation = operation,
                    ModifiedBy = modifiedBy,
                    TimestampUtc = DateTime.UtcNow,
                    BeforeData = before,
                    ExtendedProperties = normalizedExtended
                };

                // When inside a transaction: only cache; all auditlog processing happens after commit in FlushAuditLogsFireAndForget.
                if (transaction != null)
                {
                    if (_auditLogCache.Value == null)
                        _auditLogCache.Value = new List<AuditJob>();
                    _auditLogCache.Value.Add(auditJob);
                    _logger.LogDebug(
                        "Cached audit job for after-commit flush: {Table}/{Id} - Operation: {Operation}",
                        auditJob.TransactionTable, auditJob.TransactionId, auditJob.Operation);
                    return true;
                }
                else
                {
                    // No transaction provided - fallback to queue for background processing
                    _logger.LogWarning(
                        "No transaction provided for audit. Falling back to queue for {Table}/{Id}",
                        entityName,
                        transactionId);
                    
                    var enqueued = _auditQueue.TryEnqueue(auditJob);
                    if (!enqueued)
                    {
                        _logger.LogWarning(
                            "Failed to enqueue audit job for {Table}/{Id}",
                            entityName,
                            transactionId);
                    }
                    return enqueued;
                }
            }
            catch (Exception ex)
            {
                // If we're in a transaction, re-throw to ensure transaction rollback
                // Otherwise, log and return false for queue-based operations
                if (transaction != null)
                {
                    _logger.LogError(
                        ex,
                        "Error writing audit record in transaction for {EntityName}/{Id}. Transaction will rollback.",
                        entityName,
                        transactionId);
                    throw; // Re-throw to cause transaction rollback
                }
                else
                {
                    // Swallow exception for queue-based operations - audit failure should not impact main operation
                    _logger.LogError(
                        ex,
                        "Error in audit service for {EntityName}/{Id}",
                        entityName,
                        transactionId);
                    return false;
                }
            }
        }

        private static Dictionary<string, object?>? NormalizeExtendedProperties(
            Dictionary<string, object?>? source,
            Guid transactionId)
        {
            if (source == null || source.Count == 0)
                return source;

            var result = new Dictionary<string, object?>(source.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in source)
            {
                if (kvp.Value is string s)
                {
                    var trimmed = s.Trim();

                    // Special keyword: "now" → current UTC timestamp
                    if (trimmed.Equals("now", StringComparison.OrdinalIgnoreCase))
                    {
                        result[kvp.Key] = DateTime.UtcNow;
                        continue;
                    }

                    // Special keyword: "|RENGUID|" → use current transaction id (row id)
                    if (trimmed.Equals("|RENGUID|", StringComparison.Ordinal))
                    {
                        result[kvp.Key] = transactionId;
                        continue;
                    }
                }

                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        private static async Task<string?> ResolveChangeUserDisplayNameAsync(
            string modifiedBy,
            IDbConnection connection,
            IDbTransaction? transaction,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                return null;

            if (!Guid.TryParse(modifiedBy, out var userId))
                return null;

            if (connection is not NpgsqlConnection npgConn)
                return null;

            try
            {
                const string sql = @"
                    SELECT firstname, lastname
                    FROM public.portaluser
                    WHERE id = @id :: uuid
                    LIMIT 1";

                await using var cmd = transaction is NpgsqlTransaction npgTx
                    ? new NpgsqlCommand(sql, npgConn, npgTx)
                    : new NpgsqlCommand(sql, npgConn);

                cmd.Parameters.Add(new NpgsqlParameter("@id", DbType.Guid) { Value = userId });

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var first = reader.IsDBNull(0) ? null : reader.GetString(0);
                    var last = reader.IsDBNull(1) ? null : reader.GetString(1);
                    await reader.CloseAsync().ConfigureAwait(false);

                    var parts = new[] { first, last }
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToArray();

                    if (parts.Length == 0)
                        return null;

                    return string.Join(" ", parts);
                }

                await reader.CloseAsync().ConfigureAwait(false);
                return null;
            }
            catch
            {
                // Best-effort only - if lookup fails, keep original modifiedBy (GUID)
                return null;
            }
        }

        /// <summary>
        /// All auditlog processing runs here, after commit: new connection, build changelog, INSERT into auditlog. Fire-and-forget.
        /// </summary>
        public void FlushAuditLogsFireAndForget(string connectionString)
        {
            FlushAuditLogsFireAndForget(connectionString, _auditLogCache.Value);
        }

        /// <summary>
        /// Flushes the given list of audit jobs (or current cache if jobs is null). Use the list returned from PrepareAuditCacheForRequest() so flush is reliable after commit.
        /// </summary>
        public void FlushAuditLogsFireAndForget(string connectionString, List<AuditJob>? jobs)
        {
            var toUse = jobs ?? _auditLogCache.Value;
            if (toUse == null || toUse.Count == 0)
                return;

            // Snapshot and clear only if we're using the cache (so same context can be reused)
            var toFlush = new List<AuditJob>(toUse);
            if (jobs == null)
                _auditLogCache.Value = null;

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync().ConfigureAwait(false);
                    foreach (var job in toFlush)
                    {
                        try
                        {
                            await WriteAuditOnNewConnectionAsync(job, connection).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error writing audit record (after commit) for {Table}/{Id}. Continuing with remaining records.",
                                job.TransactionTable, job.TransactionId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing audit log cache after commit. {Count} record(s) may not have been written.", toFlush.Count);
                }
            });
        }

        /// <summary>
        /// Prepares the per-request audit cache. Call at the start of the transaction scope; pass the returned list to FlushAuditLogsFireAndForget after commit.
        /// </summary>
        public List<AuditJob> PrepareAuditCacheForRequest()
        {
            var list = new List<AuditJob>();
            _auditLogCache.Value = list;
            _tableDisplayNameCache.Value = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private sealed class FieldMetadata
        {
            public string FieldName { get; init; } = string.Empty;
            public string ColumnName { get; init; } = string.Empty;
            public string? DisplayName { get; init; }
            public string? LookupTable { get; init; }
            public string? LabelColumn { get; init; }
            public string? LabelIdColumn { get; init; }
            public string? LableIdDataType { get; init; }
            public long? GroupOptionKey { get; init; }
        }

        private static async Task<string?> GetTableDisplayNameCachedAsync(
            string tableName,
            IDbConnection connection,
            IDbTransaction? transaction,
            CancellationToken cancellationToken)
        {
            var cache = _tableDisplayNameCache.Value ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (cache.TryGetValue(tableName, out var cached))
                return cached;

            string? displayName = null;
            try
            {
                if (connection is NpgsqlConnection npgConn)
                {
                    const string sql = @"
                        SELECT displayname
                        FROM public.applicationtable
                        WHERE LOWER(tablename) = LOWER(@tableName)
                        LIMIT 1";

                    await using var cmd = transaction is NpgsqlTransaction npgTx
                        ? new NpgsqlCommand(sql, npgConn, npgTx)
                        : new NpgsqlCommand(sql, npgConn);
                    cmd.Parameters.Add(new NpgsqlParameter("@tableName", DbType.String) { Value = tableName });
                    var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    displayName = scalar == null || scalar == DBNull.Value ? null : scalar.ToString();
                }
            }
            catch
            {
                // best-effort only: if displayname doesn't exist or query fails, keep null
                displayName = null;
            }

            cache[tableName] = displayName;
            return displayName;
        }

        /// <summary>
        /// Resolves table display name using the given connection only. Use on a dedicated connection to avoid NpgsqlOperationInProgressException.
        /// </summary>
        //private static async Task<string?> GetTableDisplayNameFromConnectionAsync(NpgsqlConnection connection, string tableName)
        //{
        //    if (string.IsNullOrWhiteSpace(tableName))
        //        return null;
        //    try
        //    {
        //        const string sql = @"
        //            SELECT displayname, idfield, displayfield
        //            FROM public.applicationtable
        //            WHERE LOWER(tablename) = LOWER(@tableName)
        //            LIMIT 1";
        //        await using var cmd = new NpgsqlCommand(sql, connection);
        //        cmd.Parameters.Add(new NpgsqlParameter("@tableName", DbType.String) { Value = tableName });
        //        var scalar = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        //        return scalar == null || scalar == DBNull.Value ? null : scalar.ToString();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public static async Task<TableMetadata?> GetTableDisplayNameFromConnectionAsync(
            NpgsqlConnection connection,
            string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return null;

            try
            {
                const string sql = @"
                    SELECT displayname, idfield, displayfield, tablename
                    FROM public.applicationtable
                    WHERE LOWER(tablename) = LOWER(@tableName)
                    LIMIT 1";

                await using var cmd = new NpgsqlCommand(sql, connection);

                cmd.Parameters.Add(new NpgsqlParameter("@tableName", DbType.String)
                {
                    Value = tableName
                });

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    return new TableMetadata
                    {
                        DisplayName = reader.IsDBNull(0) ? null : reader.GetString(0),
                        IdField = reader.IsDBNull(1) ? null : reader.GetString(1),
                        DisplayField = reader.IsDBNull(2) ? null : reader.GetString(2),
                        TableName = reader.IsDBNull(3) ? null : reader.GetString(3)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Optional: log exception
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolves change-user display name (portaluser firstname + lastname) using the given connection only. Use on a dedicated connection to avoid NpgsqlOperationInProgressException.
        /// </summary>
        private static async Task<string?> ResolveChangeUserDisplayNameFromConnectionAsync(NpgsqlConnection connection, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy) || !Guid.TryParse(modifiedBy, out var userId))
                return null;
            try
            {
                const string sql = @"
                    SELECT firstname, lastname
                    FROM public.portaluser
                    WHERE id = @id
                    LIMIT 1";
                await using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.Add(new NpgsqlParameter("@id", DbType.Guid) { Value = userId });
                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var first = reader.IsDBNull(0) ? null : reader.GetString(0);
                    var last = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var parts = new[] { first, last }.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                    return parts.Length == 0 ? null : string.Join(" ", parts);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task WriteAuditOnNewConnectionAsync(AuditJob job, NpgsqlConnection connection)
        {
            // Resolve table display name and change-user display name on this dedicated connection (no other command in progress).
            var applicationTableData = await GetTableDisplayNameFromConnectionAsync(connection, job.TransactionTable).ConfigureAwait(false);
            var modifiedByDisplay = await ResolveChangeUserDisplayNameFromConnectionAsync(connection, job.ModifiedBy).ConfigureAwait(false) ?? job.ModifiedBy;

            var enrichedJob = new AuditJob
            {
                EntityName = job.EntityName,
                TransactionTable = job.TransactionTable,
                TableDisplayName = applicationTableData?.DisplayName,
                TransactionId = job.TransactionId,
                Operation = job.Operation,
                ModifiedBy = modifiedByDisplay,
                TimestampUtc = job.TimestampUtc,
                BeforeData = job.BeforeData,
                ExtendedProperties = job.ExtendedProperties,
                TableMetadata = applicationTableData
            };

            var changeLog = await _diffBuilder.BuildChangeLogAsync(enrichedJob, CancellationToken.None).ConfigureAwait(false);
            if (changeLog.Changes == null)
                return;
            await EnrichChangeLogValuesAsync(enrichedJob, changeLog, connection).ConfigureAwait(false);
            var changeLogJson = _diffBuilder.SerializeChangeLog(changeLog);

            const string sql = @"
                INSERT INTO public.auditlog 
                    (transactiontable, transactionid, modifiedby, operation, changelog, createdon)
                VALUES 
                    (@transactiontable, @transactionid, @modifiedby, @operation, @changelog::jsonb, @createdon)";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("@transactiontable", DbType.String) { Value = job.TransactionTable });
            command.Parameters.Add(new NpgsqlParameter("@transactionid", DbType.Guid) { Value = job.TransactionId });
            command.Parameters.Add(new NpgsqlParameter("@modifiedby", DbType.String) { Value = modifiedByDisplay });
            command.Parameters.Add(new NpgsqlParameter("@operation", DbType.String) { Value = job.Operation.ToString() });
            command.Parameters.Add(new NpgsqlParameter("@changelog", DbType.String) { Value = changeLogJson });
            command.Parameters.Add(new NpgsqlParameter("@createdon", DbType.DateTimeOffset) { Value = job.TimestampUtc });

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task EnrichChangeLogValuesAsync(AuditJob job, AuditChangeLog changeLog, NpgsqlConnection connection)
        {
            try
            {
                if (changeLog.Changes == null || changeLog.Changes.Count == 0)
                    return;

                var fieldMetadata = await LoadFieldMetadataAsync(job.TransactionTable, connection).ConfigureAwait(false);
                if (fieldMetadata.Count == 0)
                    return;

                // Dictionaries for quick lookup by different keys
                var byDisplayName = new Dictionary<string, FieldMetadata>(StringComparer.OrdinalIgnoreCase);
                var byColumnName = new Dictionary<string, FieldMetadata>(StringComparer.OrdinalIgnoreCase);
                var byFieldName = new Dictionary<string, FieldMetadata>(StringComparer.OrdinalIgnoreCase);

                foreach (var meta in fieldMetadata)
                {
                    if (!string.IsNullOrWhiteSpace(meta.DisplayName))
                        byDisplayName[meta.DisplayName!] = meta;
                    if (!string.IsNullOrWhiteSpace(meta.ColumnName))
                        byColumnName[meta.ColumnName] = meta;
                    if (!string.IsNullOrWhiteSpace(meta.FieldName))
                        byFieldName[meta.FieldName] = meta;
                }

                var keys = changeLog.Changes.Keys.ToList();
                foreach (var key in keys)
                {
                    var change = changeLog.Changes[key];
                    var meta = ResolveFieldMetadataForKey(key, byDisplayName, byColumnName, byFieldName);
                    if (meta == null)
                        continue;

                    object? before = change.Before;
                    object? after = change.After;

                    // 1. Lookup table resolution (lookuptable + labelcolumn + labelidcolumn)
                    if (!string.IsNullOrWhiteSpace(meta.LookupTable)
                        && !string.IsNullOrWhiteSpace(meta.LabelColumn)
                        && !string.IsNullOrWhiteSpace(meta.LabelIdColumn))
                    {
                        before = await ResolveLookupLabelAsync(connection, meta.LookupTable!, meta.LabelIdColumn!, meta.LabelColumn!, meta.LableIdDataType ?? "uuid", before).ConfigureAwait(false);
                        after = await ResolveLookupLabelAsync(connection, meta.LookupTable!, meta.LabelIdColumn!, meta.LabelColumn!, meta.LableIdDataType ?? "uuid", after).ConfigureAwait(false);
                    }
                    // 2. Select options via groupoptionkey -> SelectOptions(GroupKey)
                    else if (meta.GroupOptionKey.HasValue)
                    {
                        before = await ResolveSelectOptionLabelAsync(connection, meta.GroupOptionKey.Value, before).ConfigureAwait(false);
                        after = await ResolveSelectOptionLabelAsync(connection, meta.GroupOptionKey.Value, after).ConfigureAwait(false);
                    }

                    // Replace entry if we changed anything
                    if (!ReferenceEquals(before, change.Before) || !ReferenceEquals(after, change.After))
                    {
                        changeLog.Changes[key] = new AuditFieldChange
                        {
                            Before = before,
                            After = after
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Best-effort enrichment only - never fail audit write because of lookup issues
                _logger.LogWarning(ex, "Failed to enrich audit changelog values for {Table}/{Id}", job.TransactionTable, job.TransactionId);
            }
        }

        private async Task<List<FieldMetadata>> LoadFieldMetadataAsync(string entityName, NpgsqlConnection connection)
        {
            var result = new List<FieldMetadata>();

            const string sql = @"
                SELECT fieldname, columnname, displayname, lookuptable, labelcolumn, lableidcolumn, groupoptionkey, lableiddatatype
                FROM fieldmapper
                WHERE entityname = @entityName AND isactive = TRUE";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.Add(new NpgsqlParameter("@entityName", DbType.String) { Value = entityName });

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var meta = new FieldMetadata
                {
                    FieldName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    ColumnName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LookupTable = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LabelColumn = reader.IsDBNull(4) ? null : reader.GetString(4),
                    LabelIdColumn = reader.IsDBNull(5) ? null : reader.GetString(5),
                    GroupOptionKey = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6),
                    LableIdDataType = reader.IsDBNull(7) ? null : reader.GetString(7)
                };
                result.Add(meta);
            }

            await reader.CloseAsync().ConfigureAwait(false);
            return result;
        }

        private static FieldMetadata? ResolveFieldMetadataForKey(
            string key,
            Dictionary<string, FieldMetadata> byDisplayName,
            Dictionary<string, FieldMetadata> byColumnName,
            Dictionary<string, FieldMetadata> byFieldName)
        {
            if (byDisplayName.TryGetValue(key, out var meta))
                return meta;
            if (byColumnName.TryGetValue(key, out meta))
                return meta;
            if (byFieldName.TryGetValue(key, out meta))
                return meta;
            return null;
        }

        private static async Task<object?> ResolveLookupLabelAsync(
            NpgsqlConnection connection,
            string lookupTable,
            string idColumn,
            string labelColumn,
            string lableiddatatype,
            object? rawValue)
        {
            if (rawValue == null)
                return null;

            try
            {
                var sql = $@"
                    SELECT {labelColumn}
                    FROM {lookupTable}
                    WHERE {idColumn} = @id :: {lableiddatatype}
                    LIMIT 1";

                await using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.Add(new NpgsqlParameter("@id", rawValue));

                var scalar = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (scalar == null || scalar == DBNull.Value)
                    return rawValue;

                return scalar;
            }
            catch
            {
                // If lookup fails, keep original value
                return rawValue;
            }
        }

        private static async Task<object?> ResolveSelectOptionLabelAsync(
            NpgsqlConnection connection,
            long groupKey,
            object? rawValue)
        {
            if (rawValue == null)
                return null;

            var valueString = rawValue.ToString();
            if (string.IsNullOrWhiteSpace(valueString))
                return rawValue;

            try
            {
                string sql;
                var cmd = new NpgsqlCommand();
                cmd.Connection = connection;

                // If value is numeric, treat as OptionKey; otherwise treat as Code
                if (long.TryParse(valueString, out var optionKey))
                {
                    sql = @"
                        SELECT Code, TranslationKey
                        FROM SelectOptions
                        WHERE GroupKey = @groupKey :: integer AND OptionKey = @optionKey :: integer
                        LIMIT 1";
                    cmd.Parameters.Add(new NpgsqlParameter("@optionKey", DbType.Int64) { Value = optionKey });
                }
                else
                {
                    sql = @"
                        SELECT Code, TranslationKey
                        FROM SelectOptions
                        WHERE GroupKey = @groupKey :: integer AND Code = @code :: integer
                        LIMIT 1";
                    cmd.Parameters.Add(new NpgsqlParameter("@code", DbType.String) { Value = valueString });
                }

                cmd.CommandText = sql;
                cmd.Parameters.Add(new NpgsqlParameter("@groupKey", DbType.Int64) { Value = groupKey });

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var code = reader.IsDBNull(0) ? null : reader.GetString(0);
                    var translationKey = reader.IsDBNull(1) ? null : reader.GetString(1);
                    await reader.CloseAsync().ConfigureAwait(false);

                    // Prefer translation key as label; fall back to code; else original
                    return (object?)(translationKey ?? code ?? valueString);
                }

                await reader.CloseAsync().ConfigureAwait(false);
                return rawValue;
            }
            catch
            {
                // If lookup fails, keep original value
                return rawValue;
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, object?>?> ReadExistingDataAsync( // Add idColumnName
            string tableName,
            Guid transactionId,
            IDbConnection connection,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default,
            string idColumnName = "id")
        {
            try
            {
                // Use parameterized query to prevent SQL injection
                var sql = $"SELECT * FROM {SanitizeTableName(tableName)} WHERE {SanitizeTableName(idColumnName)} = '{transactionId.ToString()}' LIMIT 1";

                // If we have a transaction, use it to ensure we read the correct state
                // Otherwise, create a new connection
                if (transaction != null)
                {
                    await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);
                    //command.Parameters.Add(new NpgsqlParameter("@id", System.Data.DbType.Guid) { Value = transactionId.ToString() });

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            data[fieldName] = fieldValue;
                        }

                        // Close reader immediately after reading - no longer needed
                        await reader.CloseAsync();

                        return data;
                    }

                    // Close reader if no data found
                    await reader.CloseAsync();
                }
                else
                {
                    // No transaction - use separate connection
                    await using var auditConnection = new NpgsqlConnection(((NpgsqlConnection)connection).ConnectionString);
                    await auditConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    await using var command = new NpgsqlCommand(sql, auditConnection);
                    //command.Parameters.Add(new NpgsqlParameter("@id", System.Data.DbType.Guid) { Value = transactionId.ToString() });

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            data[fieldName] = fieldValue;
                        }

                        // Close reader immediately after reading - no longer needed
                        await reader.CloseAsync();

                        return data;
                    }

                    // Close reader if no data found
                    await reader.CloseAsync();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading existing data from {Table}/{Id}", tableName, transactionId);
                return null;
            }
        }

        private string SanitizeTableName(string tableName)
        {
            if (!TableNameRegex.IsMatch(tableName))
            {
                throw new ArgumentException($"Invalid table name: {tableName}");
            }
            return tableName;
        }
    }
}
