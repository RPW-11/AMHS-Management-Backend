using Domain.Interfaces;

namespace Application.Common.Interfaces;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent);
}