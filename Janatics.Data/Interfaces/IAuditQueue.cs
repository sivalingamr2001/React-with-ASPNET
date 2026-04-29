using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces;

public interface IAuditQueue
{
    bool TryEnqueue(AuditJob job);
}
