using BB.Common;

namespace BB.EventBus.Outbox;

public sealed class OutboxMessage : BaseEntity, IEntityWithTrace
{
    public string Type { get; set; } = string.Empty;          // CLR full name của event
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public string? CreatedTraceId { get; set; }
}
