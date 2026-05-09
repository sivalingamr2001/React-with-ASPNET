using Janatics.DataEngine.Models.Audit;
using System.Data;

namespace Janatics.DataEngine.Abstractions;

public interface IAuditOutboxService
{
    Task StageAsync(IEnumerable<AuditJob> jobs, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
