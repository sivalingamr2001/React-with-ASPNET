using DataEngine.AuditService.Interface;

namespace DataEngine.AuditService.Models;

public interface IAuditQueue
{
    bool TryEnqueue(AuditJob job);
}
