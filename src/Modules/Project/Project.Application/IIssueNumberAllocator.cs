using BB.Common;

namespace Project.Application;

/// <summary>
/// Cross-module contract: cấp số issue tiếp theo (PRJ-1, PRJ-2…).
/// Atomic trong scope Project transaction; nếu Issue create fail sau đó,
/// counter vẫn advance — chấp nhận gap (giống Jira).
/// </summary>
public interface IIssueNumberAllocator
{
    Task<Result<AllocatedIssueNumber>> AllocateAsync(Guid projectId, CancellationToken ct = default);
}

public sealed record AllocatedIssueNumber(string ProjectKey, int Number)
{
    public string IssueKey => $"{ProjectKey}-{Number}";
}
