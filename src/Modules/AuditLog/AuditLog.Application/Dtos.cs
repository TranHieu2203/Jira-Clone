namespace AuditLog.Application;

public sealed record AuditEntryDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string Scope,
    Guid? ScopeId,
    string? PayloadJson,
    DateTimeOffset OccurredAt,
    string? TraceId);

/// <summary>
/// Filter cho `GET /admin/audit`. Tất cả optional. Pagination chuẩn.
/// </summary>
public sealed record SearchAuditRequest(
    Guid? ActorUserId = null,
    string? Action = null,
    string? Scope = null,
    Guid? ScopeId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageIndex = 1,
    int PageSize = 50);
