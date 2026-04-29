using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace KATCRUDServices.Core.Models
{
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Exception? Exception { get; set; }
        
        public Dictionary<string, object> Data { get; set; } = new();
    }
}