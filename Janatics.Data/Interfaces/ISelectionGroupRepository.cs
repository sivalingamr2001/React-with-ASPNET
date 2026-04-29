using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces
{
    public interface ISelectionGroupRepository
    {
        /// <summary>
        /// Create a new selection group
        /// </summary>
        Task<SelectionGroup> CreateAsync(SelectionGroupDto dto, string? createdBy = null);

        /// <summary>
        /// Update an existing selection group
        /// </summary>
        Task<SelectionGroup> UpdateAsync(long groupKey, SelectionGroupDto dto, string? modifiedBy = null);

        /// <summary>
        /// Delete a selection group (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(long groupKey, string? modifiedBy = null);

        /// <summary>
        /// Get selection group by key
        /// </summary>
        Task<SelectionGroup?> GetByKeyAsync(long groupKey);

        /// <summary>
        /// List all selection groups
        /// </summary>
        Task<List<SelectionGroupResponse>> ListAsync(bool? isGlobal = null, string? tableName = null, string? fieldName = null);

        /// <summary>
        /// Get group with its options (header with line items)
        /// </summary>
        Task<SelectionGroupWithOptions?> GetGroupWithOptionsAsync(long groupKey);

        /// <summary>
        /// Get groups for a specific table/field
        /// </summary>
        Task<List<SelectionGroupResponse>> GetTableGroupsAsync(string tableName, string? fieldName = null);

        /// <summary>
        /// Get global groups
        /// </summary>
        Task<List<SelectionGroupResponse>> GetGlobalGroupsAsync();

        /// <summary>
        /// Check if group exists
        /// </summary>
        Task<bool> ExistsAsync(long groupKey);
    }
}
