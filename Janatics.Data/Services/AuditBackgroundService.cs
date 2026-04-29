using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Background service that processes audit jobs from the queue
    /// </summary>
    public class AuditBackgroundService : BackgroundService
    {
        private readonly AuditQueue _auditQueue;
        private readonly AuditWriter _auditWriter;
        private readonly ILogger<AuditBackgroundService> _logger;

        public AuditBackgroundService(
            AuditQueue auditQueue,
            AuditWriter auditWriter,
            ILogger<AuditBackgroundService> logger)
        {
            _auditQueue = auditQueue;
            _auditWriter = auditWriter;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit Background Service started - waiting for audit jobs...");

            try
            {
                await foreach (var job in _auditQueue.Reader.ReadAllAsync(stoppingToken))
                {
                    _logger.LogInformation(
                        "Processing audit job: {Table}/{Id} - Operation: {Operation}",
                        job.TransactionTable,
                        job.TransactionId,
                        job.Operation
                    );

                    try
                    {
                        await _auditWriter.WriteAuditAsync(job, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - continue processing other jobs
                        _logger.LogError(
                            ex,
                            "Error processing audit job for {Table}/{Id}",
                            job.TransactionTable,
                            job.TransactionId
                        );
                    }
                }
                
                _logger.LogInformation("Audit Background Service completed processing all jobs");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Audit Background Service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Audit Background Service encountered a fatal error");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Audit Background Service is stopping gracefully");
            await base.StopAsync(cancellationToken);
        }
    }
}
