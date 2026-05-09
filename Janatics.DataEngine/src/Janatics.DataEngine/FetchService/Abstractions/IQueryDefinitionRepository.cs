using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IQueryDefinitionRepository
{
    Task<long> SaveAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<QueryDefinitionModel?> GetAsync(long queryNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueryDefinitionModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long queryNumber, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task RecordAuditAsync(FetchAuditLogEntry entry, CancellationToken cancellationToken = default);
}
