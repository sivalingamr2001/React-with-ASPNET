using DataEngine.ProcessService.Model;

namespace DataEngine.ProcessService.Interfaces;

public interface IProcess
{
    Task<ProcessResult> ProcessTransactionAsync(ProcessRequest request);
}
