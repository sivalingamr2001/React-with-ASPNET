using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces
{
    public interface ISelectOptionRepository
    {
        /// <summary>
        /// Create a new select option
        /// </summary>
        Task<SelectOption> CreateAsync(SelectOptionDto dto, string? createdBy = null);

        /// <summary>
        /// Update an existing select option
        /// </summary>
        Task<SelectOption> UpdateAsync(long optionKey, SelectOptionDto dto, string? modifiedBy = null);

        Task<List<SelectOption>> GetAllAsync();

        /// <summary>
        /// Delete a select option (soft delete by setting IsActive = false)
        /// </summary>
        Task<bool> DeleteAsync(long optionKey, string? modifiedBy = null);

        /// <summary>
        /// Get select option by key
        /// </summary>
        Task<SelectOption?> GetByKeyAsync(long optionKey);

        /// <summary>
        /// Get select option by code
        /// </summary>
        Task<SelectOption?> GetByCodeAsync(string code);

        /// <summary>
        /// Get code by option key
        /// </summary>
        Task<string?> GetCodeByKeyAsync(long optionKey);

        /// <summary>
        /// Get option key by code
        /// </summary>
        Task<long?> GetKeyByCodeAsync(string code);

        /// <summary>
        /// List select options with filter
        /// </summary>
        Task<List<SelectOptionResponse>> ListAsync(SelectOptionFilter filter);

        /// <summary>
        /// Get options for a specific table and field
        /// </summary>
        Task<List<SelectOptionResponse>> GetTableOptionsAsync(string tableName, string? fieldName = null, bool includeGlobal = false);

        /// <summary>
        /// Get global options
        /// </summary>
        Task<List<SelectOptionResponse>> GetGlobalOptionsAsync(string? category = null);

        /// <summary>
        /// Get options by category
        /// </summary>
        Task<List<SelectOptionResponse>> GetByCategoryAsync(string category);

        /// <summary>
        /// Get child options by parent code
        /// </summary>
        Task<List<SelectOptionResponse>> GetChildOptionsAsync(string parentCode);

        /// <summary>
        /// Bulk create select options
        /// </summary>
        Task<List<SelectOption>> BulkCreateAsync(List<SelectOptionDto> dtos, string? createdBy = null);

        /// <summary>
        /// Check if option key exists
        /// </summary>
        Task<bool> ExistsAsync(long optionKey);

        /// <summary>
        /// Check if code exists
        /// </summary>
        Task<bool> CodeExistsAsync(string code);

        /// <summary>
        /// Get hierarchical options (tree structure for cascade dropdowns)
        /// </summary>
        Task<List<HierarchicalOption>> GetHierarchicalOptionsAsync(long? groupKey = null, string? tableName = null, string? fieldName = null);

        /// <summary>
        /// Get options by group
        /// </summary>
        Task<List<SelectOptionResponse>> GetByGroupAsync(long groupKey);

        /// <summary>
        /// Get root level options (no parent)
        /// </summary>
        Task<List<SelectOptionResponse>> GetRootOptionsAsync(long? groupKey = null);

        /// <summary>
        /// Get children of a parent option (for cascade)
        /// </summary>
        Task<List<SelectOptionResponse>> GetChildrenByParentCodeAsync(string parentCode);
    }
}
