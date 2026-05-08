namespace Janatics.DataEngine.Providers.Abstractions;

public sealed class DbCapabilities : IDbCapabilities
{
    public bool SupportsBulkInsert { get; init; }
    public bool SupportsWindowFunctions { get; init; }
    public bool SupportsJsonColumns { get; init; }
    public bool SupportsReadReplica { get; init; }
    public int MaxParameterCount { get; init; } = 2000;
    public int DefaultCommandTimeoutSeconds { get; init; } = 30;
}
