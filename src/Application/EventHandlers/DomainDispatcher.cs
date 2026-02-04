using Application.Common.Interfaces;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.EventHandlers;

public class DomainDispatcher : IDomainDispatcher
{
    private readonly IServiceProvider _sp;

    public DomainDispatcher(IServiceProvider sp) => _sp = sp;

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events)
    {
        foreach (var evt in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
            var handlers = _sp.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle));
                var task = (Task)method!.Invoke(handler, [evt])!;
                await task;
            }
        }
    }
}