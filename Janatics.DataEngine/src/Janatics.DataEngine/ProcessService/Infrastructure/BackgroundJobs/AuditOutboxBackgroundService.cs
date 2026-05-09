using Janatics.DataEngine.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.Infrastructure.BackgroundJobs;

public sealed class AuditOutboxBackgroundService(
    IAuditOutboxService outboxService,
    ILogger<AuditOutboxBackgroundService> logger) : BackgroundService
{
    private readonly IAuditOutboxService _outboxService = outboxService;
    private readonly ILogger<AuditOutboxBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await _outboxService.ProcessPendingAsync(stoppingToken).ConfigureAwait(false);
                if (processed > 0)
                    _logger.LogInformation("Processed {Count} audit outbox messages.", processed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit outbox worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
        }
    }
}
