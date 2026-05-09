using Janatics.DataEngine.ProcessService.Models.RequestModels;

namespace Janatics.DataEngine.ProcessService.Abstractions;

public interface IProcessService
{
    Task<ProcessResult> ProcessTransactionAsync(ProcessRequest request);
}
