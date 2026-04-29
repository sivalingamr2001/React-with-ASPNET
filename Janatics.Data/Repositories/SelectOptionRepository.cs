using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace KATCRUDServices.Core.Repositories
{
    public class SelectOptionRepository : BaseRepository, ISelectOptionRepository
    {
        public SelectOptionRepository(IEnhancedDataProvider dataProvider, ILogger<SelectOptionRepository> logger)
            : base(dataProvider, logger)
        {
        }

        public async Task<SelectOption> CreateAsync(SelectOptionDto dto, string? createdBy = null)
        {
            return await CreateAsync(dto, createdBy, null, null);
        }

        /// <summary>
        /// Create a new select option with optional connection and transaction parameters
        /// </summary>
        public async Task<SelectOption> CreateAsync(SelectOptionDto dto, string? createdBy = null, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
             
            try
            {
                // Check if option key already exists (if provided)
                if (dto.OptionKey.HasValue && await ExistsAsync(dto.OptionKey.Value, connection, transaction))
                {
                    throw new InvalidOperationException($"Select option with key '{dto.OptionKey}' already exists");
                }

                // Check if code already exists
                if (await CodeExistsAsync(dto.Code, connection, transaction))
                {
                    throw new InvalidOperationException($"Select option with code '{dto.Code}' already exists");
                }

                string query;
                Dictionary<string, object> parameters;

                if (dto.OptionKey.HasValue)
                {
                    // Manual OptionKey provided
                    query = @"
                        INSERT INTO SelectOptions (
                            OptionKey, Code, TranslationKey, GroupKey,
                            SortOrder, IsActive, ParentCode, HierarchyLevel, Category, 
                            CreatedDate, CreatedBy
                        )
                        VALUES (
                            @optionKey, @code, @translationKey, @groupKey,
                            @sortOrder, @isActive, @parentCode, @hierarchyLevel, @category,
                            @createdDate, @createdBy
                        );
                        SELECT * FROM SelectOptions WHERE OptionKey = @optionKey;";

                    parameters = new Dictionary<string, object>
                    {
                        { "optionKey", dto.OptionKey.Value },
                        { "code", dto.Code },
                        { "translationKey", dto.TranslationKey },
                        { "groupKey", (object?)dto.GroupKey ?? DBNull.Value },
                        { "sortOrder", dto.SortOrder },
                        { "isActive", dto.IsActive },
                        { "parentCode", (object?)dto.ParentCode ?? DBNull.Value },
                        { "hierarchyLevel", dto.HierarchyLevel },
                        { "category", (object?)dto.Category ?? DBNull.Value },
                        { "createdDate", DateTime.Now },
                        { "createdBy", (object?)createdBy ?? DBNull.Value }
                    };
                }
                else
                {
                    // Auto-generate OptionKey - database will handle it
                    query = @"
                        INSERT INTO SelectOptions (
                            Code, TranslationKey, GroupKey,
                            SortOrder, IsActive, ParentCode, HierarchyLevel, Category, 
                            CreatedDate, CreatedBy
                        )
                        VALUES (
                            @code, @translationKey, @groupKey,
                            @sortOrder, @isActive, @parentCode, @hierarchyLevel, @category,
                            @createdDate, @createdBy
                        );
                        SELECT * FROM SelectOptions WHERE Id = SCOPE_IDENTITY();";

                    parameters = new Dictionary<string, object>
                    {
                        { "code", dto.Code },
                        { "translationKey", dto.TranslationKey },
                        { "groupKey", (object?)dto.GroupKey ?? DBNull.Value },
                        { "sortOrder", dto.SortOrder },
                        { "isActive", dto.IsActive },
                        { "parentCode", (object?)dto.ParentCode ?? DBNull.Value },
                        { "hierarchyLevel", dto.HierarchyLevel },
                        { "category", (object?)dto.Category ?? DBNull.Value },
                        { "createdDate", DateTime.Now },
                        { "createdBy", (object?)createdBy ?? DBNull.Value }
                    };
                }

                var result = await ExecuteQueryAsync(query, parameters, connection, transaction);

                if (result.Rows.Count == 0)
                {
                    throw new InvalidOperationException("Failed to create select option");
                }

                var option = MapToSelectOption(result.Rows[0]);
                _logger.LogInformation("Created select option: {OptionKey} - {Code}", option.OptionKey, option.Code);

                return option;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating select option");
                throw;
            }
        }

        public async Task<SelectOption> UpdateAsync(long optionKey, SelectOptionDto dto, string? modifiedBy = null)
        {
            try
            {
                var existing = await GetByKeyAsync(optionKey);
                if (existing == null)
                {
                    throw new InvalidOperationException($"Select option with key '{optionKey}' not found");
                }

                // Check if new code conflicts with existing
                if (dto.Code != existing.Code && await CodeExistsAsync(dto.Code))
                {
                    throw new InvalidOperationException($"Select option with code '{dto.Code}' already exists");
                }

                var query = @"
                    UPDATE SelectOptions
                    SET Code = @code,
                        TranslationKey = @translationKey,
                        GroupKey = @groupKey,
                        SortOrder = @sortOrder,
                        IsActive = @isActive,
                        ParentCode = @parentCode,
                        HierarchyLevel = @hierarchyLevel,
                        Category = @category,
                        ModifiedDate = @modifiedDate,
                        ModifiedBy = @modifiedBy
                    WHERE OptionKey = @optionKey;
                    SELECT * FROM SelectOptions WHERE OptionKey = @optionKey;";

                var parameters = new Dictionary<string, object>
                {
                    { "optionKey", optionKey },
                    { "code", dto.Code },
                    { "translationKey", dto.TranslationKey },
                    { "groupKey", (object?)dto.GroupKey ?? DBNull.Value },
                    { "sortOrder", dto.SortOrder },
                    { "isActive", dto.IsActive },
                    { "parentCode", (object?)dto.ParentCode ?? DBNull.Value },
                    { "hierarchyLevel", dto.HierarchyLevel },
                    { "category", (object?)dto.Category ?? DBNull.Value },
                    { "modifiedDate", DateTime.Now },
                    { "modifiedBy", (object?)modifiedBy ?? DBNull.Value }
                };

                var result = await _dataProvider.ExecuteQueryAsync(query, parameters);
                var option = MapToSelectOption(result.Rows[0]);

                _logger.LogInformation("Updated select option: {OptionKey}", optionKey);

                return option;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating select option {OptionKey}", optionKey);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long optionKey, string? modifiedBy = null)
        {
            try
            {
                var connection = await _dataProvider.GetConnectionAsync();
                var transaction = await _dataProvider.BeginTransactionAsync(connection);

                try
                {
                    var query = @"
                        UPDATE SelectOptions
                        SET IsActive = 0,
                            ModifiedDate = @modifiedDate,
                            ModifiedBy = @modifiedBy
                        WHERE OptionKey = @optionKey;";

                    var parameters = new Dictionary<string, object>
                    {
                        { "optionKey", optionKey },
                        { "modifiedDate", DateTime.Now },
                        { "modifiedBy", (object?)modifiedBy ?? DBNull.Value }
                    };

                    var rowsAffected = await _dataProvider.ExecuteNonQueryAsync(query, parameters, transaction);
                    transaction.Commit();

                    _logger.LogInformation("Deleted select option: {OptionKey}", optionKey);

                    return rowsAffected > 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting select option {OptionKey}", optionKey);
                throw;
            }
        }

        public async Task<SelectOption?> GetByKeyAsync(long optionKey)
        {
            try
            {
                var query = "SELECT * FROM SelectOptions WHERE OptionKey = @optionKey";
                var parameters = new Dictionary<string, object>
                {
                    { "optionKey", optionKey }
                };

                var result = await _dataProvider.ExecuteQueryAsync(query, parameters);

                if (result.Rows.Count == 0)
                    return null;

                return MapToSelectOption(result.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting select option by key {OptionKey}", optionKey);
                throw;
            }
        }

        public async Task<List<SelectOption>> GetAllAsync()
        {
            try
            {
                var query = "SELECT * FROM SelectOptions";

                var result = await _dataProvider.ExecuteQueryAsync(query, null);

                var list = new List<SelectOption>();

                foreach (DataRow row in result.Rows)
                {
                    list.Add(MapToSelectOption(row));
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all select options");
                throw;
            }
        }

        public async Task<SelectOption?> GetByCodeAsync(string code)
        {
            try
            {
                var query = "SELECT * FROM SelectOptions WHERE Code = @code";
                var parameters = new Dictionary<string, object>
                {
                    { "code", code }
                };

                var result = await _dataProvider.ExecuteQueryAsync(query, parameters);

                if (result.Rows.Count == 0)
                    return null;

                return MapToSelectOption(result.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting select option by code {Code}", code);
                throw;
            }
        }

        public async Task<string?> GetCodeByKeyAsync(long optionKey)
        {
            try
            {
                var connection = await _dataProvider.GetConnectionAsync();
                var transaction = await _dataProvider.BeginTransactionAsync(connection);

                try
                {
                    var query = "SELECT Code FROM SelectOptions WHERE OptionKey = @optionKey AND IsActive = 1";
                    var parameters = new Dictionary<string, object>
                    {
                        { "optionKey", optionKey }
                    };

                    var result = await _dataProvider.ExecuteScalarAsync(query, parameters, transaction);
                    transaction.Commit();
                    return result?.ToString();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting code by key {OptionKey}", optionKey);
                throw;
            }
        }

        public async Task<long?> GetKeyByCodeAsync(string code)
        {
            try
            {
                var connection = await _dataProvider.GetConnectionAsync();
                var transaction = await _dataProvider.BeginTransactionAsync(connection);

                try
                {
                    var query = "SELECT OptionKey FROM SelectOptions WHERE Code = @code AND IsActive = 1";
                    var parameters = new Dictionary<string, object>
                    {
                        { "code", code }
                    };

                    var result = await _dataProvider.ExecuteScalarAsync(query, parameters, transaction);
                    transaction.Commit();
                    return result != null ? Convert.ToInt64(result) : null;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key by code {Code}", code);
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> ListAsync(SelectOptionFilter filter)
        {
            try
            {
                var queryBuilder = new StringBuilder(@"
                    SELECT so.Code, so.TranslationKey, so.OptionKey, so.GroupKey, so.SortOrder, 
                           so.IsActive, so.ParentCode, so.HierarchyLevel, so.Category,
                           sg.GroupName
                    FROM SelectOptions so
                    LEFT JOIN SelectionGroups sg ON so.GroupKey = sg.GroupKey
                    WHERE 1=1");
                var parameters = new Dictionary<string, object>();

                if (filter.OptionKey.HasValue)
                {
                    queryBuilder.Append(" AND so.OptionKey = @optionKey");
                    parameters["optionKey"] = filter.OptionKey.Value;
                }

                if (!string.IsNullOrEmpty(filter.Code))
                {
                    queryBuilder.Append(" AND so.Code = @code");
                    parameters["code"] = filter.Code;
                }

                if (filter.GroupKey.HasValue)
                {
                    queryBuilder.Append(" AND so.GroupKey = @groupKey");
                    parameters["groupKey"] = filter.GroupKey.Value;
                }

                if (filter.IsActive.HasValue)
                {
                    queryBuilder.Append(" AND so.IsActive = @isActive");
                    parameters["isActive"] = filter.IsActive.Value;
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    queryBuilder.Append(" AND so.Category = @category");
                    parameters["category"] = filter.Category;
                }

                if (!string.IsNullOrEmpty(filter.ParentCode))
                {
                    queryBuilder.Append(" AND so.ParentCode = @parentCode");
                    parameters["parentCode"] = filter.ParentCode;
                }

                if (filter.HierarchyLevel.HasValue)
                {
                    queryBuilder.Append(" AND so.HierarchyLevel = @hierarchyLevel");
                    parameters["hierarchyLevel"] = filter.HierarchyLevel.Value;
                }

                if (filter.RootOnly == true)
                {
                    queryBuilder.Append(" AND so.ParentCode IS NULL");
                }

                if (filter.Codes != null && filter.Codes.Any())
                {
                    var codeParams = string.Join(",", filter.Codes.Select((c, i) => $"@code{i}"));
                    queryBuilder.Append($" AND so.Code IN ({codeParams})");
                    for (int i = 0; i < filter.Codes.Count; i++)
                    {
                        parameters[$"code{i}"] = filter.Codes[i];
                    }
                }

                if (filter.OptionKeys != null && filter.OptionKeys.Any())
                {
                    var keyParams = string.Join(",", filter.OptionKeys.Select((k, i) => $"@key{i}"));
                    queryBuilder.Append($" AND so.OptionKey IN ({keyParams})");
                    for (int i = 0; i < filter.OptionKeys.Count; i++)
                    {
                        parameters[$"key{i}"] = filter.OptionKeys[i];
                    }
                }

                queryBuilder.Append(" ORDER BY so.SortOrder, so.Code");

                var result = await _dataProvider.ExecuteQueryAsync(queryBuilder.ToString(), parameters);

                return result.Rows.Cast<System.Data.DataRow>()
                    .Select(MapToSelectOptionResponse)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing select options");
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetTableOptionsAsync(string tableName, string? fieldName = null, bool includeGlobal = false)
        {
            try
            {
                // Get options via SelectionGroups that match the table/field
                var queryBuilder = new StringBuilder(@"
                    SELECT so.Code, so.TranslationKey, so.OptionKey, so.GroupKey, so.SortOrder, 
                           so.IsActive, so.ParentCode, so.HierarchyLevel, so.Category,
                           sg.GroupName
                    FROM SelectOptions so
                    INNER JOIN SelectionGroups sg ON so.GroupKey = sg.GroupKey
                    WHERE so.IsActive = 1 
                      AND sg.IsActive = 1
                      AND sg.TableName = @tableName");
                
                var parameters = new Dictionary<string, object> { { "tableName", tableName } };

                if (!string.IsNullOrEmpty(fieldName))
                {
                    queryBuilder.Append(" AND sg.FieldName = @fieldName");
                    parameters["fieldName"] = fieldName;
                }

                queryBuilder.Append(" ORDER BY so.SortOrder, so.Code");

                var result = await _dataProvider.ExecuteQueryAsync(queryBuilder.ToString(), parameters);
                var options = result.Rows.Cast<System.Data.DataRow>()
                    .Select(MapToSelectOptionResponse)
                    .ToList();

                if (includeGlobal)
                {
                    var globalOptions = await GetGlobalOptionsAsync();
                    options.AddRange(globalOptions);
                }

                return options.OrderBy(o => o.SortOrder).ThenBy(o => o.Code).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table options for {TableName}", tableName);
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetGlobalOptionsAsync(string? category = null)
        {
            try
            {
                // Get options via SelectionGroups that are global
                var queryBuilder = new StringBuilder(@"
                    SELECT so.Code, so.TranslationKey, so.OptionKey, so.GroupKey, so.SortOrder, 
                           so.IsActive, so.ParentCode, so.HierarchyLevel, so.Category,
                           sg.GroupName
                    FROM SelectOptions so
                    INNER JOIN SelectionGroups sg ON so.GroupKey = sg.GroupKey
                    WHERE so.IsActive = 1 
                      AND sg.IsActive = 1
                      AND sg.IsGlobal = 1");

                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(category))
                {
                    queryBuilder.Append(" AND so.Category = @category");
                    parameters["category"] = category;
                }

                queryBuilder.Append(" ORDER BY so.SortOrder, so.Code");

                var result = await _dataProvider.ExecuteQueryAsync(queryBuilder.ToString(), parameters);
                return result.Rows.Cast<System.Data.DataRow>()
                    .Select(MapToSelectOptionResponse)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global options");
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetByCategoryAsync(string category)
        {
            try
            {
                var filter = new SelectOptionFilter
                {
                    Category = category,
                    IsActive = true
                };

                return await ListAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options by category {Category}", category);
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetChildOptionsAsync(string parentCode)
        {
            try
            {
                var filter = new SelectOptionFilter
                {
                    ParentCode = parentCode,
                    IsActive = true
                };

                return await ListAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting child options for parent {ParentCode}", parentCode);
                throw;
            }
        }

        public async Task<List<SelectOption>> BulkCreateAsync(List<SelectOptionDto> dtos, string? createdBy = null)
        {
            try
            {
                var createdOptions = new List<SelectOption>();

                foreach (var dto in dtos)
                {
                    var option = await CreateAsync(dto, createdBy);
                    createdOptions.Add(option);
                }

                _logger.LogInformation("Bulk created {Count} select options", createdOptions.Count);

                return createdOptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating select options");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(long optionKey)
        {
            return await ExistsAsync(optionKey, null, null);
        }

        /// <summary>
        /// Check if option key exists with optional connection and transaction parameters
        /// </summary>
        public async Task<bool> ExistsAsync(long optionKey, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM SelectOptions WHERE OptionKey = @optionKey";
                var parameters = new Dictionary<string, object>
                {
                    { "optionKey", optionKey }
                };

                var result = await ExecuteScalarAsync(query, parameters, connection, transaction);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if option key exists");
                throw;
            }
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            return await CodeExistsAsync(code, null, null);
        }

        /// <summary>
        /// Check if code exists with optional connection and transaction parameters
        /// </summary>
        public async Task<bool> CodeExistsAsync(string code, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM SelectOptions WHERE Code = @code";
                var parameters = new Dictionary<string, object>
                {
                    { "code", code }
                };

                var result = await ExecuteScalarAsync(query, parameters, connection, transaction);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if code exists");
                throw;
            }
        }

        private SelectOption MapToSelectOption(System.Data.DataRow row)
        {
            return new SelectOption
            {
                Id = Convert.ToInt32(row["Id"]),
                OptionKey = Convert.ToInt64(row["OptionKey"]),
                Code = row["Code"].ToString() ?? string.Empty,
                TranslationKey = row["TranslationKey"].ToString() ?? string.Empty,
                GroupKey = row["GroupKey"] != DBNull.Value ? Convert.ToInt64(row["GroupKey"]) : null,
                SortOrder = Convert.ToInt32(row["SortOrder"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                ParentCode = row["ParentCode"] != DBNull.Value ? row["ParentCode"].ToString() : null,
                HierarchyLevel = Convert.ToInt32(row["HierarchyLevel"]),
                Category = row["Category"] != DBNull.Value ? row["Category"].ToString() : null,
                CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                CreatedBy = row["CreatedBy"] != DBNull.Value ? row["CreatedBy"].ToString() : null,
                ModifiedDate = row["ModifiedDate"] != DBNull.Value ? Convert.ToDateTime(row["ModifiedDate"]) : null,
                ModifiedBy = row["ModifiedBy"] != DBNull.Value ? row["ModifiedBy"].ToString() : null
            };
        }

        public async Task<List<SelectOptionResponse>> GetByGroupAsync(long groupKey)
        {
            try
            {
                var filter = new SelectOptionFilter
                {
                    GroupKey = groupKey,
                    IsActive = true
                };

                return await ListAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options by group {GroupKey}", groupKey);
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetRootOptionsAsync(long? groupKey = null)
        {
            try
            {
                var filter = new SelectOptionFilter
                {
                    GroupKey = groupKey,
                    RootOnly = true,
                    IsActive = true
                };

                return await ListAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root options for group {GroupKey}", groupKey);
                throw;
            }
        }

        public async Task<List<SelectOptionResponse>> GetChildrenByParentCodeAsync(string parentCode)
        {
            try
            {
                var filter = new SelectOptionFilter
                {
                    ParentCode = parentCode,
                    IsActive = true
                };

                return await ListAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting children for parent {ParentCode}", parentCode);
                throw;
            }
        }

        public async Task<List<HierarchicalOption>> GetHierarchicalOptionsAsync(long? groupKey = null, string? tableName = null, string? fieldName = null)
        {
            try
            {
                var queryBuilder = new StringBuilder(@"
                    SELECT so.Code, so.TranslationKey, so.OptionKey, so.GroupKey, so.SortOrder, 
                           so.HierarchyLevel, so.ParentCode
                    FROM SelectOptions so");

                var parameters = new Dictionary<string, object>();

                if (tableName != null || fieldName != null)
                {
                    queryBuilder.Append(@"
                    INNER JOIN SelectionGroups sg ON so.GroupKey = sg.GroupKey");
                }

                queryBuilder.Append(@"
                    WHERE so.IsActive = 1");

                if (groupKey.HasValue)
                {
                    queryBuilder.Append(" AND so.GroupKey = @groupKey");
                    parameters["groupKey"] = groupKey.Value;
                }

                if (tableName != null)
                {
                    queryBuilder.Append(" AND sg.TableName = @tableName");
                    parameters["tableName"] = tableName;
                }

                if (fieldName != null)
                {
                    queryBuilder.Append(" AND sg.FieldName = @fieldName");
                    parameters["fieldName"] = fieldName;
                }

                queryBuilder.Append(" ORDER BY so.HierarchyLevel, so.SortOrder, so.Code");

                var result = await _dataProvider.ExecuteQueryAsync(queryBuilder.ToString(), parameters);

                var allOptions = result.Rows.Cast<System.Data.DataRow>()
                    .Select(row => new HierarchicalOption
                    {
                        Code = row["Code"].ToString() ?? string.Empty,
                        TranslationKey = row["TranslationKey"].ToString() ?? string.Empty,
                        OptionKey = Convert.ToInt64(row["OptionKey"]),
                        GroupKey = row["GroupKey"] != DBNull.Value ? Convert.ToInt64(row["GroupKey"]) : null,
                        SortOrder = Convert.ToInt32(row["SortOrder"]),
                        HierarchyLevel = Convert.ToInt32(row["HierarchyLevel"]),
                        ParentCode = row["ParentCode"] != DBNull.Value ? row["ParentCode"].ToString() : null,
                        Children = new List<HierarchicalOption>()
                    })
                    .ToList();

                // Build hierarchical structure
                var rootOptions = allOptions.Where(o => o.ParentCode == null).ToList();
                var childrenMap = allOptions.Where(o => o.ParentCode != null)
                    .GroupBy(o => o.ParentCode)
                    .ToDictionary(g => g.Key!, g => g.ToList());

                foreach (var root in rootOptions)
                {
                    BuildHierarchy(root, childrenMap);
                }

                return rootOptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchical options");
                throw;
            }
        }

        private void BuildHierarchy(HierarchicalOption parent, Dictionary<string, List<HierarchicalOption>> childrenMap)
        {
            if (childrenMap.TryGetValue(parent.Code, out var children))
            {
                parent.Children = children.OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToList();
                foreach (var child in parent.Children)
                {
                    BuildHierarchy(child, childrenMap);
                }
            }
        }

        private SelectOptionResponse MapToSelectOptionResponse(System.Data.DataRow row)
        {
            return new SelectOptionResponse
            {
                Code = row["Code"].ToString() ?? string.Empty,
                TranslationKey = row["TranslationKey"].ToString() ?? string.Empty,
                OptionKey = Convert.ToInt64(row["OptionKey"]),
                GroupKey = row["GroupKey"] != DBNull.Value ? Convert.ToInt64(row["GroupKey"]) : null,
                GroupName = row["GroupName"] != DBNull.Value ? row["GroupName"].ToString() : null,
                SortOrder = Convert.ToInt32(row["SortOrder"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                ParentCode = row["ParentCode"] != DBNull.Value ? row["ParentCode"].ToString() : null,
                HierarchyLevel = Convert.ToInt32(row["HierarchyLevel"]),
                Category = row["Category"] != DBNull.Value ? row["Category"].ToString() : null,
                HasChildren = false // This should be set based on actual child count if needed
            };
        }
    }
}
