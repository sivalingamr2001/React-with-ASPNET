using JanaticsApi.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace JanaticsApi.API.Services;

public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string CorrelationId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
