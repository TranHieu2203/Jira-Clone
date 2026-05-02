using BB.EventBus;
using BB.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Api.Host.Infrastructure.Outbox;

public sealed class EfOutboxStore : IOutboxStore
{
    private readonly OutboxDbContext _db;

    public EfOutboxStore(OutboxDbContext db) => _db = db;

    public async Task EnqueueAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        Type t = @event.GetType();
        string typeName = t.AssemblyQualifiedName
            ?? throw new InvalidOperationException("Event type has no AssemblyQualifiedName");

        string json = System.Text.Json.JsonSerializer.Serialize(@event, t, OutboxJson.Options);

        var row = new OutboxMessage
        {
            Type = typeName,
            PayloadJson = json,
            OccurredAt = @event.OccurredAt,
            CreatedTraceId = @event.TraceId,
            RetryCount = 0
        };

        _db.OutboxMessages.Add(row);
        await _db.SaveChangesAsync(ct);
    }
}
