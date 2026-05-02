using System.Text.Json;
using Notification.Domain;

namespace Notification.Application;

internal static class Mappers
{
    internal static InAppNotificationDto ToDto(InAppNotification n)
    {
        JsonElement payload;
        try
        {
            payload = JsonSerializer.Deserialize<JsonElement>(n.PayloadJson);
        }
        catch (JsonException)
        {
            payload = JsonSerializer.Deserialize<JsonElement>("{}")!;
        }

        return new InAppNotificationDto(n.Id, n.Type, payload, n.IsRead, n.CreatedAt);
    }
}
