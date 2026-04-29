namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Configuration for DAG triggers on table CRUD operations
    /// </summary>
    public class DagTriggerConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Trigger type: "Insert", "Update", or "Scheduled"
        /// </summary>
        public string Trigger { get; set; } = string.Empty;

        /// <summary>
        /// Reference to DAG configuration ID in KTAiFlow (GUID)
        /// </summary>
        public Guid DagId { get; set; }

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Cron expression for scheduled triggers (e.g., "0 2 * * *" for daily at 2 AM)
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Trigger execution type: "DAGS" or "DirectInsert"
        /// </summary>
        public string TriggerType { get; set; } = "DAGS";

        /// <summary>
        /// JSON configuration for creating additional system records
        /// Contains renprops-like structure with |RENGUID| placeholders
        /// </summary>
        public string? InsertConfig { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
