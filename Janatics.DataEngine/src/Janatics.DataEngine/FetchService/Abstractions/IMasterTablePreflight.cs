namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IMasterTablePreflight
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
}
