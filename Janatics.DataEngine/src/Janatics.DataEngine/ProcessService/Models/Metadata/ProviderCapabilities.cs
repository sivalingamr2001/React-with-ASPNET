namespace Janatics.DataEngine.Models.Metadata;

public sealed class ProviderCapabilities
{
    public required string ProviderName { get; init; }
    public bool SupportsJson { get; init; }
    public bool SupportsRecursiveCte { get; init; }
    public bool SupportsTransactionalDdl { get; init; }
    public bool SupportsUpsert { get; init; }
    public string ParameterPrefix { get; init; } = "@";
}
