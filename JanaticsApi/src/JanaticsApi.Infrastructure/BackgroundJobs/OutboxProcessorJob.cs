// src/EnterpriseApi.Infrastructure/BackgroundJobs/OutboxProcessorJob.cs
using JanaticsApi.Infrastructure.Outbox;
using JanaticsApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JanaticsApi.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

    public OutboxProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    private async Task ProcessOutboxMessages(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredOn)
            .Take(20) // Process in batches
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);

                if (eventType is null)
                {
                    _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                    message.MarkFailed("Unknown event type.");
                    continue;
                }

                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType);

                if (domainEvent is null)
                {
                    message.MarkFailed("Failed to deserialize event.");
                    continue;
                }

                await publisher.Publish(domainEvent, ct);
                message.MarkProcessed();

                _logger.LogInformation(
                    "Processed outbox message {MessageId} of type {Type}",
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}