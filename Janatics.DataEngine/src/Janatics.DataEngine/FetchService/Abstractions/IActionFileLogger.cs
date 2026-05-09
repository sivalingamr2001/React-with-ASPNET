namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IActionFileLogger
{
    Task LogAsync(
        string serviceName,
        string actionName,
        string status,
        string details,
        string? performedBy = null,
        CancellationToken cancellationToken = default);
}
