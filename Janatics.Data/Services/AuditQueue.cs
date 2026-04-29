using System.Threading.Channels;
using KATCRUDServices.Core.Interfaces;
using KATCRUDServices.Core.Models;
using Microsoft.Extensions.Logging;

namespace KATCRUDServices.Core.Services
{
    /// <summary>
    /// Channel-based audit queue for fire-and-forget audit logging
    /// </summary>
    public class AuditQueue : IAuditQueue
    {
        private readonly Channel<AuditJob> _channel;
        private readonly ILogger<AuditQueue> _logger;

        public AuditQueue(ILogger<AuditQueue> logger, int capacity = 10000)
        {
            _logger = logger;
            
            // Create bounded channel with drop-write behavior when full
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            };
            
            _channel = Channel.CreateBounded<AuditJob>(options);
        }

        public bool TryEnqueue(AuditJob job)
        {
            if (_channel.Writer.TryWrite(job))
            {
                _logger.LogInformation(
                    "✓ Audit job enqueued: {Table}/{Id} - Operation: {Operation}",
                    job.TransactionTable,
                    job.TransactionId,
                    job.Operation
                );
                return true;
            }

            _logger.LogWarning(
                "✗ Audit queue is full. Dropping audit job for {Table}/{Id}",
                job.TransactionTable,
                job.TransactionId
            );
            return false;
        }

        /// <summary>
        /// Internal method for background service to read from queue
        /// </summary>
        internal ChannelReader<AuditJob> Reader => _channel.Reader;
    }
}
