namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Represents audit metadata for a table
    /// </summary>
    public class TableAuditMetadata
    {
        public required string TableName { get; init; }
        public bool AuditEnabled { get; init; }
    }
}
