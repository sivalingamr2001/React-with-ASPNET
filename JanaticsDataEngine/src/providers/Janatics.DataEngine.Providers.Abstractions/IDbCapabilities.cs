namespace Janatics.DataEngine.Providers.Abstractions;

public interface IDbCapabilities
{
    bool SupportsBulkInsert { get; }
    bool SupportsWindowFunctions { get; }
    bool SupportsJsonColumns { get; }
    bool SupportsReadReplica { get; }
    int MaxParameterCount { get; }
    int DefaultCommandTimeoutSeconds { get; }
}
