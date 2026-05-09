using Janatics.DataEngine.ProcessService.Models.Audit;

namespace Janatics.DataEngine.ProcessService.Abstractions;

public interface IAuditQueue
{
    bool TryEnqueue(AuditJob job);
}
