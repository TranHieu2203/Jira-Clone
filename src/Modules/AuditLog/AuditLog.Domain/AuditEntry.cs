using BB.Common;

namespace AuditLog.Domain;

/// <summary>
/// Audit entry append-only. Không edit/delete sau khi tạo (immutable).
/// </summary>
public sealed class AuditEntry : BaseEntity
{
    public const int ActionMaxLength = 80;
    public const int ScopeMaxLength = 32;
    public const int PayloadMaxLength = 4000;

    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public Guid? ScopeId { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? TraceId { get; private set; }

    private AuditEntry() { }

    public AuditEntry(Guid? actorUserId, string action, string scope, Guid? scopeId, string? payloadJson, DateTimeOffset occurredAt, string? traceId)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("action required", nameof(action));
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("scope required", nameof(scope));

        ActorUserId = actorUserId;
        Action = action.Trim();
        Scope = scope.Trim();
        ScopeId = scopeId;
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson)
            ? null
            : (payloadJson.Length > PayloadMaxLength ? payloadJson[..PayloadMaxLength] : payloadJson);
        OccurredAt = occurredAt;
        TraceId = traceId;
    }
}
