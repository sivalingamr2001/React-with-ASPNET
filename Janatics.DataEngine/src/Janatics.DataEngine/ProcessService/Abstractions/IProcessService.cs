using Janatics.DataEngine.Models.RequestModels;

namespace Janatics.DataEngine.Abstractions;

public interface IProcessService
{
    Task<ProcessResult> ProcessTransactionAsync(ProcessRequest request);
}
