// src/EnterpriseApi.API/Extensions/RateLimitingExtensions.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace JanaticsApi.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please retry after the indicated delay."
                }, ct);
            };

            // Per-IP global rate limit
            options.AddPolicy("PerIp", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Per-user rate limit for authenticated requests (higher limits for trusted users)
            options.AddPolicy("PerUser", context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    });
            });

            // Strict rate limit for authentication endpoints
            options.AddPolicy("AuthEndpoints", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}