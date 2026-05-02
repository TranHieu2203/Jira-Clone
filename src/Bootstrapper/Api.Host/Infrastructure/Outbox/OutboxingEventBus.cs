using BB.EventBus;

namespace Api.Host.Infrastructure.Outbox;

/// <summary><see cref="IEventBus"/> ghi vào outbox; processor gọi <see cref="InMemoryEventBus"/> để chạy handler.</summary>
public sealed class OutboxingEventBus : IEventBus
{
    private readonly IOutboxStore _store;

    public OutboxingEventBus(IOutboxStore store) => _store = store;

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent =>
        _store.EnqueueAsync(@event, ct);
}
