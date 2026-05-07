// src/EnterpriseApi.Infrastructure/BackgroundJobs/TokenCleanupJob.cs
using JanaticsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace JanaticsApi.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class TokenCleanupJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TokenCleanupJob> _logger;

    public TokenCleanupJob(ApplicationDbContext context, ILogger<TokenCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Token cleanup job starting...");

        var cutoff = DateTimeOffset.UtcNow;

        var deletedCount = await _context.Users
            .Where(u => u.RefreshTokenExpiresAt < cutoff)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.RefreshToken, (string?)null)
                 .SetProperty(u => u.RefreshTokenExpiresAt, (DateTimeOffset?)null),
                context.CancellationToken);

        _logger.LogInformation(
            "Token cleanup complete. Cleared {Count} expired refresh tokens.", deletedCount);
    }
}

// Registration in DI
services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("token-cleanup");

q.AddJob<TokenCleanupJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("token-cleanup-trigger")
        .WithCronSchedule("0 0 2 * * ?") // Daily at 2 AM UTC
        .StartNow());
});

services.AddQuartzHostedService(options =>
    options.WaitForJobsToComplete = true);