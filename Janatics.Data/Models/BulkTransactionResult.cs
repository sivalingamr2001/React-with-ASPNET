using Newtonsoft.Json;

namespace KATCRUDServices.Core.Models
{
    public class BulkTransactionResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("batchId")]
        public string BatchId { get; set; } = string.Empty;

        [JsonProperty("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonProperty("successfulRecords")]
        public int SuccessfulRecords { get; set; }

        [JsonProperty("failedRecords")]
        public int FailedRecords { get; set; }

        [JsonProperty("successResults")]
        public List<TransactionResult> SuccessResults { get; set; } = new();

        [JsonProperty("failedResults")]
        public List<BulkTransactionError> FailedResults { get; set; } = new();

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }
    }

    public class BulkTransactionError
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonProperty("recordIndex")]
        public int RecordIndex { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonProperty("data")]
        public Dictionary<string, object>? Data { get; set; }

        [JsonProperty("exception")]
        public Exception? Exception { get; set; }
    }
}
