namespace Janatics.DataEngine.ProcessService.Models.Execution;

public sealed class ProcessExecutionContext
{
    public string RootEntityId { get; init; } = string.Empty;
    public string RootEntityName { get; init; } = string.Empty;
    public ProcessExecutionState State { get; set; } = ProcessExecutionState.Received;
    public int Depth { get; set; }
    public List<ExecutionStep> Steps { get; } = new();
}
