using Domain.Interfaces;

namespace Application.Common.Interfaces;

public interface IDomainDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events);
}