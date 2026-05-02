using System.Text.Json;

namespace Notification.Application;

public sealed record InAppNotificationDto(
    Guid Id,
    string Type,
    JsonElement Payload,
    bool IsRead,
    DateTimeOffset CreatedAt);
