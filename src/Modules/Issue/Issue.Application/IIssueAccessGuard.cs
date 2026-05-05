namespace Issue.Application;

/// <summary>
/// Snapshot tối thiểu cho việc kiểm tra quyền truy cập issue.
/// </summary>
public sealed record IssueAccessSnapshot(Guid IssueId, Guid ProjectId);

/// <summary>
/// Cổng kiểm tra quyền truy cập một issue cụ thể (theo thành viên project).
/// Dùng cho các module cross-cutting (Comment, Attachment, ActivityLog) — chặn enumeration `issueId` lạ.
/// </summary>
public interface IIssueAccessGuard
{
    /// <summary>
    /// Trả snapshot (issueId + projectId) nếu user là thành viên project sở hữu issue.
    /// Trả null trong cả 2 trường hợp:
    /// (a) issue không tồn tại;
    /// (b) issue tồn tại nhưng user không là thành viên project — KHÔNG leak existence để tránh enumeration attack.
    /// Caller xử lý null là 404 `issue.not_found`.
    /// </summary>
    Task<IssueAccessSnapshot?> ResolveAccessAsync(Guid userId, Guid issueId, CancellationToken ct = default);
}
