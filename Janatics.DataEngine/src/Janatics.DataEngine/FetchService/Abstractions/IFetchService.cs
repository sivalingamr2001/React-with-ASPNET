using Janatics.DataEngine.FetchService.Models;

namespace Janatics.DataEngine.FetchService.Abstractions;

public interface IFetchService
{
    Task<QueryDefinitionModel> SaveQueryAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<QueryDefinitionModel?> GetQueryAsync(long queryNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueryDefinitionModel>> GetQueriesAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteQueryAsync(long queryNumber, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<FetchExecutionResult> ExecuteQueryAsync(ExecuteStoredQueryRequest request, CancellationToken cancellationToken = default);
    Task<FetchExecutionResult> ExecuteAsync(FetchExecutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TableMetadataModel>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ColumnMetadataModel>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken = default);
}
