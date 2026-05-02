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

public sealed record SearchIssuesRequest(
    Guid? ProjectId, Guid? IssueTypeId, Guid? AssigneeId, Guid? ReporterId,
    Guid? CurrentStatusId, int? Priority, string? TextSearch, bool? IncludeArchived,
    int PageIndex = 1, int PageSize = 50, string? Sort = null);
