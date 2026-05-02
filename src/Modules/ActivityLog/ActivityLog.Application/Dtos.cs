using System.Text.Json;

namespace ActivityLog.Application;

public sealed record ActivityItemDto(
    Guid Id,
    Guid IssueId,
    DateTimeOffset OccurredAt,
    string Kind,
    Guid? ActorUserId,
    JsonElement? Payload);
