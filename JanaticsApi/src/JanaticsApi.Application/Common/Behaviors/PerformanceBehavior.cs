// src/EnterpriseApi.Application/Common/Behaviors/PerformanceBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JanaticsApi.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMilliseconds}ms. Request: {@Request}",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}