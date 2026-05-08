using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Domain.Entities;

public sealed class QueryDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantCode { get; init; } = default!;
    public string QueryKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string SqlTemplate { get; init; } = default!;
    public string RootEntityKey { get; init; } = default!;
    public DataProviderType ProviderType { get; init; }
    public string ConnectionName { get; init; } = default!;
    public bool IsPaginationEnabled { get; init; }
    public bool IsCacheable { get; init; }
    public int CacheTtlSeconds { get; init; }
    public int Version { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
    public IReadOnlyList<QueryChildDefinition> Children { get; init; } = [];
}
