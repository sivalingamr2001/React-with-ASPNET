using Newtonsoft.Json;

namespace Janatics.DataEngine.Models.RequestModels;

public class ProcessRequest
{
    [JsonProperty("entityProps")]
    public Dictionary<string, object> EntityProperties { get; set; } = new();

    [JsonProperty("nodeProps")]
    public Dictionary<string, List<Dictionary<string, object>>> NodeProps { get; set; } = new();

    [JsonProperty("delProps")]
    public Dictionary<string, List<Dictionary<string, object>>> DelProps { get; set; } = new();

    [JsonProperty("rootEntityName")]
    public string RootEntityName { get; set; } = string.Empty;

    [JsonProperty("rootEntityId")]
    public string RootEntityId { get; set; } = string.Empty;

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

public class ProcessResult
{
    public bool Success { get; set; }
    public string RootEntityId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Exception? Exception { get; set; }

    public Dictionary<string, object> Data { get; set; } = new();
}
