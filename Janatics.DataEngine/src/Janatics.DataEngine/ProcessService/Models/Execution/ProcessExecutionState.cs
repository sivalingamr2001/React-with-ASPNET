namespace Janatics.DataEngine.Models.Execution;

public enum ProcessExecutionState
{
    Received,
    Validated,
    ProcessingRoot,
    ProcessingChildren,
    ProcessingDeletes,
    Completed,
    Failed
}
