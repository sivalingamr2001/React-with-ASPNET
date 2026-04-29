using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Builds changelog JSON from audit job data.
    /// When FieldMapper config exists for the entity, only fields defined in FieldMapper are included in the changelog.
    /// </summary>
    public class AuditDiffBuilder
    {
        private readonly ILogger<AuditDiffBuilder> _logger;
        private readonly FieldMapperService _fieldMapperService;
        private static readonly AsyncLocal<IDataProvider?> _asyncLocalDataProvider = new AsyncLocal<IDataProvider?>();
        private static IDataProvider? _globalDataProvider;

        private static IDataProvider? CurrentProvider => _asyncLocalDataProvider.Value ?? _globalDataProvider;

        // Standard audit fields to always ignore
        private static readonly HashSet<string> StandardAuditFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedOn", "UpdatedOn", "CreatedBy", "UpdatedBy", "Version",
            "createdon", "updatedon", "createdby", "updatedby", "version"
        };

        public AuditDiffBuilder(ILogger<AuditDiffBuilder> logger, FieldMapperService fieldMapperService, IDataProvider dataProvider)
        {
            _logger = logger;
            _fieldMapperService = fieldMapperService;
            if (_globalDataProvider == null)
            {
                _globalDataProvider = dataProvider;
                Console.WriteLine("[DagTriggerRepository] Global Data provider set");
            }
            // Always set the scoped wrapper for the current execution context
            _asyncLocalDataProvider.Value = dataProvider;
        }

        /// <summary>
        /// Builds a changelog object from audit job data. Only fields defined in FieldMapper for the entity are included.
        /// </summary>
        public async Task<AuditChangeLog> BuildChangeLogAsync(AuditJob job, CancellationToken cancellationToken = default)
        {
            HashSet<string>? allowedColumnNames = null;
            HashSet<string>? allowedFieldNames = null;

            Dictionary<string, string>? fieldNameToColumnName = null;
            Dictionary<string, string>? columnNameToDisplayName = null;
            Dictionary<string, string>? fieldNameToDisplayName = null;
            try
            {
                var mappers = await _fieldMapperService.GetFieldMappersAsync(job.TransactionTable, transaction: null).ConfigureAwait(false);
                if (mappers != null && mappers.Count > 0)
                {
                    allowedColumnNames = new HashSet<string>(mappers.Select(m => m.ColumnName), StringComparer.OrdinalIgnoreCase);
                    allowedFieldNames = new HashSet<string>(mappers.Select(m => m.FieldName), StringComparer.OrdinalIgnoreCase);
                    fieldNameToColumnName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    columnNameToDisplayName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    fieldNameToDisplayName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var m in mappers)
                    {
                        fieldNameToColumnName[m.FieldName] = m.ColumnName;
                        if (!string.IsNullOrWhiteSpace(m.DisplayName))
                        {
                            columnNameToDisplayName[m.ColumnName] = m.DisplayName!;
                            fieldNameToDisplayName[m.FieldName] = m.DisplayName!;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load field mappers for entity {Entity}. Audit changelog will include all fields.", job.TransactionTable);
            }

            return await BuildChangeLogAsync(job, allowedColumnNames, allowedFieldNames, fieldNameToColumnName, columnNameToDisplayName, fieldNameToDisplayName);
        }

        /// <summary>
        /// Builds a changelog object from audit job data. When allowed sets are provided, only those keys are included (field mapper data only).
        /// </summary>
        public async Task<AuditChangeLog> BuildChangeLogAsync(
         AuditJob job,
         HashSet<string>? allowedColumnNames = null,
         HashSet<string>? allowedFieldNames = null,
         Dictionary<string, string>? fieldNameToColumnName = null,
         Dictionary<string, string>? columnNameToDisplayName = null,
         Dictionary<string, string>? fieldNameToDisplayName = null)
        {
            var changes = new Dictionary<string, AuditFieldChange>();
            var allowAll = allowedColumnNames == null && allowedFieldNames == null;

            bool IsAllowedKey(string key) =>
                allowAll ||
                (allowedColumnNames?.Contains(key) == true) ||
                (allowedFieldNames?.Contains(key) == true);

            // ✅ Get display record name (cleaned)
            var displayRecordName = await GetDisplayRecordNameAsync(job);

            // ✅ Build changes based on operation
            switch (job.Operation)
            {
                case AuditOperation.CREATE:
                    BuildCreateChanges(job.ExtendedProperties, StandardAuditFields, changes,
                        IsAllowedKey, fieldNameToColumnName, columnNameToDisplayName, fieldNameToDisplayName);
                    break;

                case AuditOperation.UPDATE:
                    BuildUpdateChanges(job.BeforeData, job.ExtendedProperties, StandardAuditFields, changes,
                        IsAllowedKey, fieldNameToColumnName, columnNameToDisplayName, fieldNameToDisplayName);
                    break;

                case AuditOperation.DELETE:
                    BuildDeleteChanges(job.BeforeData, StandardAuditFields, changes,
                        allowedColumnNames, allowAll, columnNameToDisplayName);
                    break;
            }

            return new AuditChangeLog
            {
                ChangeUser = job.ModifiedBy,
                Timestamp = job.TimestampUtc,
                Table = job.TableDisplayName ?? job.TransactionTable,
                TableDisplayName = job.TableDisplayName,
                TransactionId = job.TransactionId,
                Operation = job.Operation.ToString(),
                Changes = changes.Count > 0 ? changes : null,
                DisplayRecordName = displayRecordName,
            };
        }

        private async Task<string> GetDisplayRecordNameAsync(AuditJob job)
        {
            var displayField = job.TableMetadata?.DisplayField;

            if (string.IsNullOrWhiteSpace(displayField))
                return "";

            // 1. From ExtendedProperties
            var value = GetValue(job.ExtendedProperties, displayField);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            // 2. From BeforeData (only for non-create)
            if (job.Operation != AuditOperation.CREATE)
            {
                value = GetValue(job.BeforeData, displayField);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            // 3. From database
            if (job.Operation != AuditOperation.CREATE &&
                !string.IsNullOrWhiteSpace(job.TableMetadata?.TableName) && CurrentProvider != null)
            {
                try
                {
                    var query = $"SELECT {displayField} FROM {job.TableMetadata.TableName} WHERE {job.TableMetadata.IdField} = '{job.TransactionId.ToString()}'";

                    var parameters = new Dictionary<string, object>
                    {
                        { "@id", job.TransactionId.ToString() }
                    };

                    var result = await CurrentProvider.ExecuteQueryAsync(query, parameters);

                    if (result?.Rows?.Count > 0)
                    {
                        return result.Rows[0]?[displayField]?.ToString() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to query display record name for entity {Entity}",
                        job.TransactionTable);
                }
            }

            return "";
        }

        private static string GetValue(IDictionary<string, object>? source, string key)
        {
            return source != null &&
                   source.TryGetValue(key, out var value) &&
                   value != null
                ? value.ToString() ?? ""
                : "";
        }

        private void BuildCreateChanges(
            Dictionary<string, object?>? extendedProperties,
            HashSet<string> ignoreFields,
            Dictionary<string, AuditFieldChange> changes,
            Func<string, bool>? isAllowedKey,
            Dictionary<string, string>? fieldNameToColumnName,
            Dictionary<string, string>? columnNameToDisplayName,
            Dictionary<string, string>? fieldNameToDisplayName)
        {
            if (extendedProperties == null) return;
            isAllowedKey ??= _ => true;

            foreach (var kvp in extendedProperties)
            {
                if (ignoreFields.Contains(kvp.Key)) continue;
                if (!isAllowedKey(kvp.Key)) continue;

                var afterValue = NormalizeValue(kvp.Value);
                if (afterValue == null) continue;

                var columnKey = fieldNameToColumnName != null && fieldNameToColumnName.TryGetValue(kvp.Key, out var col)
                    ? col
                    : kvp.Key;
                var displayName = ResolveDisplayName(kvp.Key, columnKey, columnNameToDisplayName, fieldNameToDisplayName);
                var changeKey = displayName ?? columnKey;

                changes[changeKey] = new AuditFieldChange
                {
                    Before = null,
                    After = afterValue
                };
            }
        }

        private void BuildUpdateChanges(
            Dictionary<string, object?>? beforeData,
            Dictionary<string, object?>? extendedProperties,
            HashSet<string> ignoreFields,
            Dictionary<string, AuditFieldChange> changes,
            Func<string, bool>? isAllowedKey,
            Dictionary<string, string>? fieldNameToColumnName,
            Dictionary<string, string>? columnNameToDisplayName,
            Dictionary<string, string>? fieldNameToDisplayName)
        {
            if (extendedProperties == null) return;
            isAllowedKey ??= _ => true;

            foreach (var kvp in extendedProperties)
            {
                if (ignoreFields.Contains(kvp.Key)) continue;
                if (!isAllowedKey(kvp.Key)) continue;

                var afterValue = NormalizeValue(kvp.Value);
                var columnKey = fieldNameToColumnName != null && fieldNameToColumnName.TryGetValue(kvp.Key, out var col)
                    ? col
                    : kvp.Key;
                var beforeValue = beforeData != null && beforeData.TryGetValue(columnKey, out var bv)
                    ? NormalizeValue(bv)
                    : null;

                if (beforeValue == null && afterValue == null) continue;
                if (ValuesEqual(beforeValue, afterValue)) continue;
                if (afterValue == null) continue;

                var displayName = ResolveDisplayName(kvp.Key, columnKey, columnNameToDisplayName, fieldNameToDisplayName);
                var changeKey = displayName ?? columnKey;

                changes[changeKey] = new AuditFieldChange
                {
                    Before = beforeValue,
                    After = afterValue
                };
            }
        }

        private void BuildDeleteChanges(
            Dictionary<string, object?>? beforeData,
            HashSet<string> ignoreFields,
            Dictionary<string, AuditFieldChange> changes,
            HashSet<string>? allowedColumnNames,
            bool allowAll,
            Dictionary<string, string>? columnNameToDisplayName)
        {
            if (beforeData == null) return;

            foreach (var kvp in beforeData)
            {
                if (ignoreFields.Contains(kvp.Key)) continue;
                if (!allowAll && (allowedColumnNames == null || !allowedColumnNames.Contains(kvp.Key))) continue;

                var beforeValue = NormalizeValue(kvp.Value);
                if (beforeValue == null) continue;

                var displayName = ResolveDisplayName(fieldKey: null, columnKey: kvp.Key, columnNameToDisplayName, fieldNameToDisplayName: null);
                var changeKey = displayName ?? kvp.Key;

                changes[changeKey] = new AuditFieldChange
                {
                    Before = beforeValue,
                    After = null
                };
            }
        }

        private static string? ResolveDisplayName(
            string? fieldKey,
            string columnKey,
            Dictionary<string, string>? columnNameToDisplayName,
            Dictionary<string, string>? fieldNameToDisplayName)
        {
            if (columnNameToDisplayName != null && columnNameToDisplayName.TryGetValue(columnKey, out var colName) && !string.IsNullOrWhiteSpace(colName))
                return colName;
            if (fieldKey != null && fieldNameToDisplayName != null && fieldNameToDisplayName.TryGetValue(fieldKey, out var fieldName) && !string.IsNullOrWhiteSpace(fieldName))
                return fieldName;
            return null;
        }

        private object? NormalizeValue(object? value)
        {
            if (value == null || value is DBNull) return null;

            // Convert DateTime to ISO 8601 string
            if (value is DateTime dt)
            {
                return dt.ToUniversalTime().ToString("O");
            }

            // Convert DateTimeOffset to ISO 8601 string
            if (value is DateTimeOffset dto)
            {
                return dto.ToUniversalTime().ToString("O");
            }

            return value;
        }

        private bool ValuesEqual(object? value1, object? value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            // Use string comparison for normalized values
            return value1.ToString() == value2.ToString();
        }

        /// <summary>
        /// Converts changelog to JSON string for storage
        /// </summary>
        public string SerializeChangeLog(AuditChangeLog changeLog)
        {
            try
            {
                return JsonSerializer.Serialize(changeLog, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize changelog");
                throw;
            }
        }
    }
}
