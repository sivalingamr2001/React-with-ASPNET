using Newtonsoft.Json;

namespace KATCRUDServices.Core.Models
{
    public class BulkTransactionRequest
    {
        [JsonProperty("transactions")]
        public List<TransactionRequest> Transactions { get; set; } = new();

        [JsonProperty("batchId")]
        public string BatchId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("continueOnError")]
        public bool ContinueOnError { get; set; } = true;
    }
}
