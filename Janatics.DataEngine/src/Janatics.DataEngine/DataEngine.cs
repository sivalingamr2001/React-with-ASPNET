using Janatics.DataEngine.FetchService.Abstractions;
using Janatics.DataEngine.FetchService.Models;
using Janatics.DataEngine.ProcessService.Abstractions;
using Janatics.DataEngine.ProcessService.Models.RequestModels;
using Microsoft.Extensions.DependencyInjection;

namespace Janatics.DataEngine;

public class DataEngine(IProcessService processService, IAuditService auditService, IFetchService fetchService, IServiceProvider serviceProvider)
{
    private readonly IProcessService _processService = processService;
    private readonly IAuditService _auditService = auditService;
    private readonly IFetchService _fetchService = fetchService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    // Add to DataEngine.cs
    public async Task InitializeSchemaAsync(CancellationToken cancellationToken = default)
    {
        // Trigger warm-up manually
        var preflight = _serviceProvider.GetRequiredService<IMasterTablePreflight>();
        await preflight.EnsureReadyAsync(cancellationToken);
    }

    /// <summary>
    /// The primary method to run data operations
    /// </summary>
    public async Task ExecuteAsync(ProcessRequest request)
    {
        // World-class logic: Audit -> Process -> Log
        await _processService.ProcessTransactionAsync(request);
    }

    public Task<QueryDefinitionModel> SaveFetchQueryAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default)
        => _fetchService.SaveQueryAsync(request, cancellationToken);

    public Task<QueryDefinitionModel> CreateFetchQueryAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        request.QueryNumber = null;
        return _fetchService.SaveQueryAsync(request, cancellationToken);
    }

    public Task<QueryDefinitionModel> UpdateFetchQueryAsync(SaveQueryDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.QueryNumber.HasValue || request.QueryNumber.Value <= 0)
            throw new ArgumentException("QueryNumber is required for update.", nameof(request));

        return _fetchService.SaveQueryAsync(request, cancellationToken);
    }

    public Task<QueryDefinitionModel?> GetFetchQueryAsync(long queryNumber, CancellationToken cancellationToken = default)
        => _fetchService.GetQueryAsync(queryNumber, cancellationToken);

    public Task<IReadOnlyList<QueryDefinitionModel>> GetFetchQueriesAsync(CancellationToken cancellationToken = default)
        => _fetchService.GetQueriesAsync(cancellationToken);

    public Task<bool> DeleteFetchQueryAsync(long queryNumber, string? deletedBy = null, CancellationToken cancellationToken = default)
        => _fetchService.DeleteQueryAsync(queryNumber, deletedBy, cancellationToken);

    public Task<FetchExecutionResult> ExecuteFetchQueryAsync(ExecuteStoredQueryRequest request, CancellationToken cancellationToken = default)
        => _fetchService.ExecuteQueryAsync(request, cancellationToken);

    public Task<FetchExecutionResult> ExecuteFetchAsync(FetchExecutionRequest request, CancellationToken cancellationToken = default)
        => _fetchService.ExecuteAsync(request, cancellationToken);

    public Task<IReadOnlyList<TableMetadataModel>> GetFetchTablesAsync(CancellationToken cancellationToken = default)
        => _fetchService.GetTablesAsync(cancellationToken);

    public Task<IReadOnlyList<ColumnMetadataModel>> GetFetchTableColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        => _fetchService.GetTableColumnsAsync(tableName, cancellationToken);
}
