// src/EnterpriseApi.Domain/Common/IDomainEvent.cs
using MediatR;

namespace JanaticsApi.Domain.Common;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}