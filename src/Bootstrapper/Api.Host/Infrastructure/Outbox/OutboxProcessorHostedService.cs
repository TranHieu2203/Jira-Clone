using System.Reflection;
using BB.EventBus;
using BB.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Host.Infrastructure.Outbox;

/// <summary>Quét bảng outbox định kỳ, deserialize và gọi <see cref="IEventBus.PublishAsync{TEvent}"/>.</summary>
public sealed class OutboxProcessorHostedService : BackgroundService
{
    private static readonly MethodInfo PublishAsyncDefinition =
        typeof(InMemoryEventBus).GetMethods()
            .Single(m => m.Name == nameof(InMemoryEventBus.PublishAsync)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2);

    public const int PollSeconds = 5;
    public const int BatchSize = 50;
    public const int MaxRetriesBeforeDeadLetter = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    public OutboxProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started (poll every {Seconds}s)", PollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor batch failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(PollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        OutboxDbContext db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        InMemoryEventBus bus = scope.ServiceProvider.GetRequiredService<InMemoryEventBus>();

        List<OutboxMessage> pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetriesBeforeDeadLetter)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        foreach (OutboxMessage msg in pending)
        {
            await DispatchOne(db, bus, msg, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task DispatchOne(OutboxDbContext db, InMemoryEventBus bus, OutboxMessage msg, CancellationToken ct)
    {
        Type? eventType = Type.GetType(msg.Type, throwOnError: false);
        if (eventType is null)
        {
            msg.RetryCount++;
            msg.Error = $"Type.GetType failed: {msg.Type}";
            _logger.LogWarning("Outbox {Id}: unknown type {Type}", msg.Id, msg.Type);
            return;
        }

        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        {
            msg.RetryCount++;
            msg.Error = "Payload type does not implement IIntegrationEvent";
            return;
        }

        object? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize(msg.PayloadJson, eventType, OutboxJson.Options);
        }
        catch (Exception ex)
        {
            msg.RetryCount++;
            msg.Error = Truncate(ex.Message, 4000);
            _logger.LogWarning(ex, "Outbox {Id}: deserialize failed", msg.Id);
            return;
        }

        if (payload is not IIntegrationEvent)
        {
            msg.RetryCount++;
            msg.Error = "Deserialized payload is not IIntegrationEvent";
            return;
        }

        try
        {
            MethodInfo publish = PublishAsyncDefinition.MakeGenericMethod(eventType);
            Task task = (Task)publish.Invoke(bus, new[] { payload, ct })!;
            await task.ConfigureAwait(false);
            msg.ProcessedAt = DateTimeOffset.UtcNow;
            msg.Error = null;
        }
        catch (Exception ex)
        {
            msg.RetryCount++;
            msg.Error = Truncate(ex.Message, 4000);
            _logger.LogWarning(ex, "Outbox {Id}: publish/handler failed", msg.Id);
        }

        if (msg.RetryCount >= MaxRetriesBeforeDeadLetter && msg.ProcessedAt is null)
        {
            msg.ProcessedAt = DateTimeOffset.UtcNow;
            msg.Error = Truncate((msg.Error ?? "") + " [DEAD_LETTER_MAX_RETRY]", 4000);
            _logger.LogError("Outbox {Id} moved to dead-letter after {Retries} retries", msg.Id, msg.RetryCount);
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
