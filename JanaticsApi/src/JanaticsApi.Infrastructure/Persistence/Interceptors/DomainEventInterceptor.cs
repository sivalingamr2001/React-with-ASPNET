// src/EnterpriseApi.Infrastructure/Persistence/Interceptors/DomainEventInterceptor.cs
using JanaticsApi.Domain.Common;
using JanaticsApi.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JanaticsApi.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // Convert domain events to outbox messages for reliable delivery
        var outboxMessages = eventData.Context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(e => e.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            })
            .Select(OutboxMessage.Create)
            .ToList();

        if (outboxMessages.Count > 0)
        {
            await eventData.Context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}