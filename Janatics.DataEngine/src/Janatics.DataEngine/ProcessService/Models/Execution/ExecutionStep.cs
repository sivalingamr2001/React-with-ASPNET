namespace Janatics.DataEngine.ProcessService.Models.Execution;

public sealed class ExecutionStep
{
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}
