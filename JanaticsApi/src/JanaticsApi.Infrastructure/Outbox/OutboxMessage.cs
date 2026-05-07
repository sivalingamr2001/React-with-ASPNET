// src/EnterpriseApi.Infrastructure/Outbox/OutboxMessage.cs
using System.Text.Json;

namespace JanaticsApi.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent,
                new JsonSerializerOptions { WriteIndented = false }),
            OccurredOn = domainEvent.OccurredOn
        };
    }

    public void MarkProcessed() => ProcessedOn = DateTimeOffset.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}