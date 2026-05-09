using Janatics.DataEngine.ProcessService.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.ProcessService.Infrastructure.BackgroundJobs;

public sealed class AuditOutboxBackgroundService(
    IServiceScopeFactory scopeFactory, // Inject factory instead of the scoped service
    ILogger<AuditOutboxBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<AuditOutboxBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a scope to resolve Scoped services (like DB contexts)
                using (var scope = _scopeFactory.CreateScope())
                {
                    var outboxService = scope.ServiceProvider.GetRequiredService<IAuditOutboxService>();

                    var processed = await outboxService.ProcessPendingAsync(stoppingToken).ConfigureAwait(false);

                    if (processed > 0)
                        _logger.LogInformation("Processed {Count} audit outbox messages.", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit outbox worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
        }
    }
}
