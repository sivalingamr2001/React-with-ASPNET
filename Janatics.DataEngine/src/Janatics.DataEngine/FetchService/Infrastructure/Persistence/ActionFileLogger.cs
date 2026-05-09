using System.Text;
using Janatics.DataEngine.FetchService.Abstractions;

namespace Janatics.DataEngine.FetchService.Infrastructure.Persistence;

public sealed class ActionFileLogger(DataEngineOptions options) : IActionFileLogger
{
    private readonly DataEngineOptions _options = options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task LogAsync(
        string serviceName,
        string actionName,
        string status,
        string details,
        string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        var logsFolderPath = ResolveLogsFolderPath();
        Directory.CreateDirectory(logsFolderPath);

        var logFilePath = Path.Combine(logsFolderPath, $"data-engine-{DateTime.UtcNow:yyyyMMdd}.log");
        var line = BuildLine(serviceName, actionName, status, details, performedBy);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(logFilePath, line, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string ResolveLogsFolderPath()
    {
        if (Path.IsPathRooted(_options.LogsFolderPath))
            return _options.LogsFolderPath;

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _options.LogsFolderPath));
    }

    private static string BuildLine(string serviceName, string actionName, string status, string details, string? performedBy)
    {
        var actor = string.IsNullOrWhiteSpace(performedBy) ? "system" : performedBy.Trim();
        return $"{DateTimeOffset.UtcNow:O} | service={serviceName} | action={actionName} | status={status} | by={actor} | details={details}{Environment.NewLine}";
    }
}
