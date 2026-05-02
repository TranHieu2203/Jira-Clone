using Issue.Domain;

namespace Issue.Application;

internal static class Mappers
{
    public static IssueDto ToDto(Domain.Issue i) => new(
        i.Id, i.ProjectId, i.Key, i.Number, i.IssueTypeId, i.WorkflowId, i.CurrentStatusId,
        i.Summary, i.Description, (int)i.Priority,
        i.ReporterId, i.AssigneeId, i.ParentIssueId,
        i.Labels.ToList(), i.DueDate, i.StoryPoints,
        i.OriginalEstimateMinutes, i.RemainingEstimateMinutes, i.TimeSpentMinutes,
        i.IsArchived,
        i.Watchers.Select(w => w.UserId).ToList(),
        i.CreatedAt, i.UpdatedAt);

    public static IssueSummaryDto ToSummary(Domain.Issue i) =>
        new(
            i.Id,
            i.ProjectId,
            i.Key,
            i.IssueTypeId,
            i.CurrentStatusId,
            i.Summary,
            (int)i.Priority,
            i.AssigneeId,
            i.CreatedAt);
}
