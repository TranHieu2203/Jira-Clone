using BB.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BB.EventBus;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return;

        using var scope = _sp.CreateScope();
        foreach (var @event in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                try
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                        .Invoke(handler, new object?[] { @event, ct })!;
                    await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Domain event handler {Handler} failed for {Event}",
                        handler.GetType().Name, @event.GetType().Name);
                }
            }
        }
    }
}
