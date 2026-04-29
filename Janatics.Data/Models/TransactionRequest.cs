using Newtonsoft.Json;

namespace KATCRUDServices.Core.Models
{
    public class TransactionRequest
    {
        [JsonProperty("extendedProperties")]
        public Dictionary<string, object> ExtendedProperties { get; set; } = new();

        [JsonProperty("renProps")]
        public Dictionary<string, List<Dictionary<string, object>>> RenProps { get; set; } = new();

        [JsonProperty("delProps")]
        public Dictionary<string, List<Dictionary<string, object>>> DelProps { get; set; } = new();

        [JsonProperty("transactionEntityName")]
        public string TransactionEntityName { get; set; } = string.Empty;

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("isApprovalCreation")]
        public bool IsApprovalCreation { get; set; } = false;

        /// <summary>
        /// When true, uses direct model property to column binding without field mappers.
        /// Property names in ExtendedProperties map directly to database column names.
        /// </summary>
        [JsonProperty("useModelBinding")]
        public bool UseModelBinding { get; set; } = false;
    }

    public class TriggerEntry
    {
        public string DagId { get; set; }
        public string ChildTableName { get; set; }
        public string ChildRecordId { get; set; }
        public Dictionary<string, object>? ChildDagContext { get; set; }
        public Func<string, string?, string?, int?, int?, Task> OnChildExecutionComplete { get; set; }
    }
}