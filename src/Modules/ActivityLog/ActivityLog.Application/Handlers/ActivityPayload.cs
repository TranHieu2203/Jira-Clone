using System.Text.Json;

namespace ActivityLog.Application.Handlers;

internal static class ActivityPayload
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string? Serialize(object? payload) =>
        payload is null ? null : JsonSerializer.Serialize(payload, JsonOptions);
}
