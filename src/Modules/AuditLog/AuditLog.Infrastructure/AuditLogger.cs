using System.Diagnostics;
using System.Text.Json;
using AuditLog.Domain;
using BB.Security;
using Microsoft.Extensions.Logging;

namespace AuditLog.Infrastructure;

/// <summary>
/// EF-backed implementation. Best-effort: log warning thay vì throw nếu DB lỗi.
/// Lý do: audit không nên block business action — caller có thể đã commit transaction.
/// </summary>
public sealed class EfAuditLogger : IAuditLogger
{
    private readonly AuditLogDbContext _ctx;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EfAuditLogger> _logger;

    public EfAuditLogger(AuditLogDbContext ctx, ICurrentUser currentUser, ILogger<EfAuditLogger> logger)
    {
        _ctx = ctx;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task LogAsync(string action, string scope, Guid? scopeId, object? payload, CancellationToken ct = default)
    {
        try
        {
            string? json = payload is null
                ? null
                : JsonSerializer.Serialize(payload, JsonOptions);

            string? traceId = Activity.Current?.TraceId.ToString();

            var entry = new AuditEntry(
                _currentUser.UserId,
                action,
                scope,
                scopeId,
                json,
                DateTimeOffset.UtcNow,
                traceId);

            _ctx.Entries.Add(entry);
            await _ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit không block business — log + bỏ qua.
            _logger.LogWarning(ex, "Audit log failed for action={Action} scope={Scope} scopeId={ScopeId}", action, scope, scopeId);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}
