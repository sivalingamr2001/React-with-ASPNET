namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Represents a selection group (header) that contains multiple select options
    /// </summary>
    public class SelectionGroup
    {
        public int Id { get; set; }
        public long GroupKey { get; set; }  // Auto-generated BIGINT starting from 5000
        public string GroupName { get; set; } = string.Empty;
        public string? TableName { get; set; }  // Specific table or NULL for global
        public string? FieldName { get; set; }  // Specific field or NULL for global
        public bool IsGlobal { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating selection groups
    /// </summary>
    public class SelectionGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string? FieldName { get; set; }
        public bool IsGlobal { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Response for selection group queries
    /// </summary>
    public class SelectionGroupResponse
    {
        public long GroupKey { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string? FieldName { get; set; }
        public bool IsGlobal { get; set; }
        public int OptionCount { get; set; }  // Number of options in this group
    }

    /// <summary>
    /// Group with its options (header with line items)
    /// </summary>
    public class SelectionGroupWithOptions
    {
        public SelectionGroupResponse Group { get; set; } = new();
        public List<SelectOptionResponse> Options { get; set; } = new();
    }
}
