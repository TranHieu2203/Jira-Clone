using BB.Common;

namespace Workflow.Application.Engine;

public sealed record TransitionOutcome(
    Guid IssueId,
    Guid? FromStatusId,
    Guid ToStatusId,
    Guid TransitionId,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> FieldChanges);

/// <summary>
/// Engine thực thi một transition. Module Issue gọi vào đây.
/// Engine KHÔNG ghi Issue.StatusId (Issue module chịu) — chỉ:
///   - Verify transition hợp lệ
///   - Run rules / validators / post-functions
///   - Trả về TransitionOutcome (status mới + field changes) để caller áp dụng
///   - Tạo IssueStatusHistory entry
/// </summary>
public interface IWorkflowEngine
{
    Task<Result<TransitionOutcome>> TransitionAsync(
        Guid issueId,
        Guid projectId,
        Guid issueTypeId,
        Guid currentStatusId,
        Guid transitionId,
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? inputs = null,
        string? comment = null,
        CancellationToken ct = default);

    /// <summary>List các transition khả thi từ status hiện tại (sau khi pass rules).</summary>
    Task<Result<IReadOnlyList<AvailableTransition>>> GetAvailableTransitionsAsync(
        Guid projectId,
        Guid issueTypeId,
        Guid currentStatusId,
        Guid currentUserId,
        CancellationToken ct = default);
}

public sealed record AvailableTransition(
    Guid Id,
    string Name,
    Guid ToStatusId,
    string ToStatusName,
    Guid? ScreenId);
