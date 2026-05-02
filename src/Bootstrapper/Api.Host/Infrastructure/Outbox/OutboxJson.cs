using System.Text.Json;

namespace Api.Host.Infrastructure.Outbox;

internal static class OutboxJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
