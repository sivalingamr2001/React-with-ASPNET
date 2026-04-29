namespace KATCRUDServices.Core.Models
{
    /// <summary>
    /// Log entry for DAG trigger execution
    /// Stores context data and response for audit and debugging
    /// </summary>
    public class DagTriggerLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TriggerId { get; set; }

        public Guid DagId { get; set; }

        public string TableName { get; set; } = string.Empty;

        public string TriggerType { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        /// <summary>
        /// JSON data passed to the DAG (input context)
        /// </summary>
        public string? ContextData { get; set; }

        /// <summary>
        /// JSON response from the DAG execution
        /// </summary>
        public string? ResponseData { get; set; }

        /// <summary>
        /// Execution status: Pending, Success, Failed
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Time taken to execute the DAG in milliseconds
        /// </summary>
        public int? ExecutionTimeMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExecutedAt { get; set; }
    }
}
