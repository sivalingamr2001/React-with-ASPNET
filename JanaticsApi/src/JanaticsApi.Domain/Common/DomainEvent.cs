// src/EnterpriseApi.Domain/Common/DomainEvent.cs
using JanaticsApi.Domain.Common;

namespace JanaticsApi.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}