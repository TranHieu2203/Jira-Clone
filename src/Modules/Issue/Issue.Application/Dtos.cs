using System.Text.Json;

namespace Issue.Application;

public sealed record IssueDto(
    Guid Id,
    Guid ProjectId,
    string Key,
    int Number,
    Guid IssueTypeId,
    Guid WorkflowId,
    Guid CurrentStatusId,
    string Summary,
    string? Description,
    int Priority,
    Guid ReporterId,
    Guid? AssigneeId,
    Guid? ParentIssueId,
    IReadOnlyList<string> Labels,
    DateTimeOffset? DueDate,
    decimal? StoryPoints,
    int? OriginalEstimateMinutes,
    int? RemainingEstimateMinutes,
    int? TimeSpentMinutes,
    bool IsArchived,
    IReadOnlyList<Guid> Watchers,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record IssueSummaryDto(
    Guid Id,
    Guid ProjectId,
    string Key,
    Guid IssueTypeId,
    Guid CurrentStatusId,
    string Summary,
    int Priority,
    Guid? AssigneeId,
    DateTimeOffset CreatedAt);

public sealed record CreateIssueRequest(
    Guid ProjectId,
    Guid IssueTypeId,
    string Summary,
    string? Description,
    int? Priority,
    Guid? AssigneeId,
    Guid? ParentIssueId,
    DateTimeOffset? DueDate,
    decimal? StoryPoints,
    List<string>? Labels,
    /// <summary>Optional custom field values, gọi qua IIssueFieldValueService sau khi issue tạo xong.</summary>
    Dictionary<Guid, JsonElement>? CustomFieldValues);

public sealed record UpdateIssueRequest(
    string Summary,
    string? Description,
    int? Priority,
    Guid? AssigneeId,
    Guid? ParentIssueId,
    DateTimeOffset? DueDate,
    decimal? StoryPoints,
    List<string>? Labels,
    int? OriginalEstimateMinutes,
    int? RemainingEstimateMinutes,
    int? TimeSpentMinutes);

public sealed record TransitionIssueRequest(
    Guid TransitionId,
    Dictionary<string, JsonElement>? Inputs,
    string? Comment);

public sealed record IssueFieldFilterRequest(
    Guid CustomFieldId,
    string? IndexedStringEquals,
    decimal? IndexedNumberEquals,
    DateTimeOffset? IndexedDateEquals);

public sealed record SearchIssuesRequest(
    Guid? ProjectId, Guid? IssueTypeId, Guid? AssigneeId, Guid? ReporterId,
    Guid? CurrentStatusId, int? Priority, string? TextSearch, bool? IncludeArchived,
    int PageIndex = 1, int PageSize = 50, string? Sort = null,
    string? Jql = null,
    List<IssueFieldFilterRequest>? FieldFilters = null,
    List<Guid>? IssueIds = null,
    List<Guid>? ExcludeIssueIds = null);

// ─────────── F5: Bulk edit ───────────────────────────────────────

/// <summary>
/// Bulk operations áp dụng cho 1 list issueId. Mỗi field optional — caller chỉ điền field
/// muốn thay đổi. Status transition cố tình KHÔNG có ở đây — mỗi issue có thể đang ở status
/// khác nhau với workflow khác nhau, cần validate per-issue ở UI riêng.
/// </summary>
public sealed record BulkUpdateOperationsDto(
    /// <summary>Set assignee. Null + ClearAssignee=false → không đổi.</summary>
    Guid? AssigneeId = null,
    /// <summary>Khi true → unset assignee (override AssigneeId).</summary>
    bool ClearAssignee = false,
    /// <summary>Set priority (1-5). Null = không đổi.</summary>
    int? Priority = null,
    /// <summary>Labels add (union, case-insensitive dedup).</summary>
    IReadOnlyList<string>? AddLabels = null,
    /// <summary>Labels remove (case-insensitive).</summary>
    IReadOnlyList<string>? RemoveLabels = null,
    /// <summary>True = archive, false = unarchive, null = không đổi.</summary>
    bool? Archive = null);

public sealed record BulkUpdateRequest(
    IReadOnlyList<Guid> IssueIds,
    BulkUpdateOperationsDto Operations);

/// <summary>1 dòng failure cho 1 issueId không apply được — caller hiển thị message theo i18n.</summary>
public sealed record BulkUpdateFailureDto(Guid IssueId, string MessageKey);

public sealed record BulkUpdateResultDto(
    IReadOnlyList<Guid> Succeeded,
    IReadOnlyList<BulkUpdateFailureDto> Failed);
