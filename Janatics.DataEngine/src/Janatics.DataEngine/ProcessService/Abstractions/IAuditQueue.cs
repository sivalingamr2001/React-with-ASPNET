using Janatics.DataEngine.Models.Audit;

namespace Janatics.DataEngine.Abstractions;

public interface IAuditQueue
{
    bool TryEnqueue(AuditJob job);
}
