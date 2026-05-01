using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BB.EventBus;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string? TraceId { get; }
}

public abstract record IntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, string? TraceId) : IIntegrationEvent;

public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent;
}

public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(IServiceProvider sp, ILogger<InMemoryEventBus> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
    {
        using var scope = _sp.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var h in handlers)
        {
            try
            {
                await h.HandleAsync(@event, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event handler {Handler} failed for {Event}", h.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}

public interface IOutboxStore
{
    Task EnqueueAsync(IIntegrationEvent @event, CancellationToken ct = default);
}
