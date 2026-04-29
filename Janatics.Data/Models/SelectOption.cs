namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Represents a select option with translation support
    /// </summary>
    public class SelectOption
    {
        public int Id { get; set; }
        public long OptionKey { get; set; }  // Auto-generated BIGINT
        public string Code { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public long? GroupKey { get; set; }  // FK to SelectionGroups(GroupKey)
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ParentCode { get; set; }  // For parent-child relationships
        public int HierarchyLevel { get; set; } = 0;  // 0 = root, 1 = child, 2 = grandchild, etc.
        public string? Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating select options
    /// </summary>
    public class SelectOptionDto
    {
        public long? OptionKey { get; set; }  // Nullable - auto-generated if null
        public string Code { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public long? GroupKey { get; set; }  // FK to SelectionGroups(GroupKey)
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? ParentCode { get; set; }
        public int HierarchyLevel { get; set; } = 0;
        public string? Category { get; set; }
    }

    /// <summary>
    /// Filter for querying select options
    /// </summary>
    public class SelectOptionFilter
    {
        public long? OptionKey { get; set; }
        public long? GroupKey { get; set; }  // Filter by group key
        public string? Code { get; set; }
        public bool? IsActive { get; set; }
        public string? Category { get; set; }
        public string? ParentCode { get; set; }
        public int? HierarchyLevel { get; set; }  // Filter by hierarchy level
        public bool? RootOnly { get; set; }  // Get only root level options (no parent)
        public List<string>? Codes { get; set; }
        public List<long>? OptionKeys { get; set; }
    }

    /// <summary>
    /// Response for select option queries
    /// </summary>
    public class SelectOptionResponse
    {
        public string Code { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public long OptionKey { get; set; }
        public long? GroupKey { get; set; }
        public string? GroupName { get; set; }  // Populated via join
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string? ParentCode { get; set; }
        public int HierarchyLevel { get; set; }
        public bool HasChildren { get; set; }  // Indicates if this option has child options
        public string? Category { get; set; }
    }

    /// <summary>
    /// Grouped select options by table/field
    /// </summary>
    public class SelectOptionGroup
    {
        public string TableName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public List<SelectOptionResponse> Options { get; set; } = new();
    }

    /// <summary>
    /// Hierarchical option with children (for cascade dropdowns)
    /// </summary>
    public class HierarchicalOption
    {
        public string Code { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public long OptionKey { get; set; }
        public long? GroupKey { get; set; }
        public int SortOrder { get; set; }
        public int HierarchyLevel { get; set; }
        public string? ParentCode { get; set; }
        public List<HierarchicalOption> Children { get; set; } = new();
    }
}
