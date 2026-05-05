using System.Text.Json;
using ActivityLog.Domain;
using BB.Common;
using BB.Security;
using FluentValidation;
using Issue.Application.Repositories;
using Sprint.Application.Repositories;
using Sprint.Domain;
using SprintEntity = global::Sprint.Domain.Sprint;
using Workflow.Application.Repositories;
using Workflow.Domain;

namespace Sprint.Application;

public sealed class SprintService : ISprintService
{
    private readonly ISprintRepository _sprints;
    private readonly ISprintUnitOfWork _uow;
    private readonly IIssueRepository _issues;
    private readonly IWorkflowRepository _workflows;
    private readonly ActivityLog.Application.Repositories.IActivityEntryRepository _activities;
    private readonly IPermissionChecker _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateSprintRequest> _createValidator;
    private readonly IValidator<UpdateSprintRequest> _updateValidator;
    private readonly IValidator<ReorderSprintIssuesRequest> _reorderValidator;

    public SprintService(
        ISprintRepository sprints,
        ISprintUnitOfWork uow,
        IIssueRepository issues,
        IWorkflowRepository workflows,
        ActivityLog.Application.Repositories.IActivityEntryRepository activities,
        IPermissionChecker permissions,
        ICurrentUser currentUser,
        IValidator<CreateSprintRequest> createValidator,
        IValidator<UpdateSprintRequest> updateValidator,
        IValidator<ReorderSprintIssuesRequest> reorderValidator)
    {
        _sprints = sprints;
        _uow = uow;
        _issues = issues;
        _workflows = workflows;
        _activities = activities;
        _permissions = permissions;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _reorderValidator = reorderValidator;
    }

    public async Task<Result<IReadOnlyList<SprintDto>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<SprintDto>>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.ProjectView, ct))
            return Result.Failure<IReadOnlyList<SprintDto>>(ErrorType.Forbidden, "project.access_denied");

        var list = await _sprints.ListByProjectAsync(projectId, ct);
        return Result.Success<IReadOnlyList<SprintDto>>(list.Select(ToDto).ToList());
    }

    public async Task<Result<SprintDto>> GetByIdAsync(Guid projectId, Guid sprintId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.ProjectView, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);
        return Result.Success(ToDto(sp));
    }

    public async Task<Result<SprintDto?>> GetActiveAsync(Guid projectId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto?>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.ProjectView, ct))
            return Result.Failure<SprintDto?>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetActiveForProjectAsync(projectId, ct);
        return Result.Success(sp is null ? null : ToDto(sp));
    }

    public async Task<Result<SprintDto>> CreateAsync(Guid projectId, CreateSprintRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        await _createValidator.ValidateAndThrowAsync(request, ct);

        var sprint = new SprintEntity(projectId, request.Name, request.StartDate, request.EndDate, request.Goal);
        await _sprints.AddAsync(sprint, ct);
        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprint.Id, ct) ?? sprint;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.created.success");
    }

    public async Task<Result<SprintDto>> UpdateAsync(Guid projectId, Guid sprintId, UpdateSprintRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);

        try
        {
            sp.Rename(request.Name, request.Goal, request.StartDate, request.EndDate);
        }
        catch (DomainException dx)
        {
            return Result.Failure<SprintDto>(dx.ErrorType, dx.MessageKey);
        }

        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprintId, ct) ?? sp;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.updated.success");
    }

    public async Task<Result<SprintDto>> AddIssueAsync(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);

        Issue.Domain.Issue? issue = await _issues.GetByIdAsync(issueId, ct);
        if (issue is null || issue.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.Validation, SprintErrors.MsgIssueWrongProject);

        Guid? other = await _sprints.FindOpenSprintIdForIssueAsync(projectId, issueId, sprintId, ct);
        if (other.HasValue)
            return Result.Failure<SprintDto>(ErrorType.Conflict, SprintErrors.MsgIssueInOtherSprint);

        try
        {
            sp.AddIssue(issueId);
        }
        catch (DomainException dx)
        {
            return Result.Failure<SprintDto>(dx.ErrorType, dx.MessageKey);
        }

        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprintId, ct) ?? sp;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.issue_added.success");
    }

    public async Task<Result> RemoveIssueAsync(Guid projectId, Guid sprintId, Guid issueId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure(ErrorType.NotFound, SprintErrors.MsgNotFound);

        try
        {
            sp.RemoveIssue(issueId);
        }
        catch (DomainException dx)
        {
            return Result.Failure(dx.ErrorType, dx.MessageKey);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "sprint.issue_removed.success");
    }

    public async Task<Result<SprintDto>> ReorderIssuesAsync(Guid projectId, Guid sprintId, ReorderSprintIssuesRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        await _reorderValidator.ValidateAndThrowAsync(request, ct);

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);

        try
        {
            sp.ReorderIssues(request.IssueIds);
        }
        catch (DomainException dx)
        {
            return Result.Failure<SprintDto>(dx.ErrorType, dx.MessageKey);
        }

        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprintId, ct) ?? sp;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.reordered.success");
    }

    public async Task<Result<SprintDto>> StartAsync(Guid projectId, Guid sprintId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);

        if (await _sprints.HasOtherActiveSprintAsync(projectId, sprintId, ct))
            return Result.Failure<SprintDto>(ErrorType.Conflict, SprintErrors.MsgActiveExists);

        var lines = new List<SprintCommitLine>();
        foreach (SprintIssue row in sp.Items.OrderBy(x => x.Rank))
        {
            Issue.Domain.Issue? issue = await _issues.GetByIdAsync(row.IssueId, ct);
            if (issue is null)
                continue;

            Workflow.Domain.Workflow? wf = await _workflows.GetWithDetailsAsync(issue.WorkflowId, ct);
            Workflow.Domain.WorkflowStatus? st = wf?.Statuses.FirstOrDefault(s => s.Id == issue.CurrentStatusId);
            bool doneAtStart = st?.Category == StatusCategory.Done;
            decimal pts = doneAtStart ? 0 : (issue.StoryPoints ?? 1m);
            lines.Add(new SprintCommitLine(sp.Id, row.IssueId, pts));
        }

        try
        {
            sp.Start();
        }
        catch (DomainException dx)
        {
            return Result.Failure<SprintDto>(dx.ErrorType, dx.MessageKey);
        }

        await _sprints.AddCommitLinesAsync(lines, ct);
        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprintId, ct) ?? sp;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.started.success");
    }

    public async Task<Result<SprintDto>> CompleteAsync(Guid projectId, Guid sprintId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.IssueEdit, ct))
            return Result.Failure<SprintDto>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);

        try
        {
            sp.Complete();
            sp.ClearIssueLinks();
        }
        catch (DomainException dx)
        {
            return Result.Failure<SprintDto>(dx.ErrorType, dx.MessageKey);
        }

        await _uow.SaveChangesAsync(ct);
        var reloaded = await _sprints.GetWithItemsAsync(sprintId, ct) ?? sp;
        return Result.Success(ToDto(reloaded), messageKey: "sprint.completed.success");
    }

    public async Task<Result<SprintBurndownDto>> GetBurndownAsync(Guid projectId, Guid sprintId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SprintBurndownDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.ProjectView, ct))
            return Result.Failure<SprintBurndownDto>(ErrorType.Forbidden, "project.access_denied");

        var sp = await _sprints.GetWithItemsAsync(sprintId, ct);
        if (sp is null || sp.ProjectId != projectId)
            return Result.Failure<SprintBurndownDto>(ErrorType.NotFound, SprintErrors.MsgNotFound);
        if (sp.Status != SprintStatus.Active && sp.Status != SprintStatus.Completed)
            return Result.Failure<SprintBurndownDto>(ErrorType.Validation, SprintErrors.MsgBurndownRequiresActiveOrCompleted);

        IReadOnlyList<SprintCommitLine> lines = await _sprints.ListCommitLinesAsync(sprintId, ct);
        decimal total = lines.Sum(x => x.BurndownPoints);
        if (total <= 0)
        {
            var singleDay = new List<BurndownDayDto>
            {
                new(DateOnly.FromDateTime(sp.StartDate.UtcDateTime).ToString("O"), 0, 0)
            };
            return Result.Success(new SprintBurndownDto(sprintId, 0, singleDay));
        }

        Dictionary<Guid, decimal> issuePoints = lines.ToDictionary(x => x.IssueId, x => x.BurndownPoints);
        List<Guid> issueIds = lines.Select(x => x.IssueId).Distinct().ToList();

        DateTimeOffset chartEnd = sp.Status == SprintStatus.Active
            ? DateTimeOffset.UtcNow
            : sp.EndDate;
        if (chartEnd > sp.EndDate)
            chartEnd = sp.EndDate;

        IReadOnlyList<ActivityEntry> acts = await _activities.ListIssueStatusChangesForIssuesAsync(
            issueIds,
            sp.StartDate,
            chartEnd,
            ct);

        Dictionary<Guid, Workflow.Domain.Workflow?> wfCache = new();
        IReadOnlyList<Issue.Domain.Issue> issueEntities = await _issues.ListAsync(i => issueIds.Contains(i.Id), ct);
        Dictionary<Guid, Issue.Domain.Issue> issueMap = issueEntities.ToDictionary(i => i.Id);

        Dictionary<Guid, DateTimeOffset?> firstDoneAt = issueIds.ToDictionary(id => id, _ => (DateTimeOffset?)null);

        foreach (ActivityEntry a in acts.OrderBy(x => x.OccurredAt))
        {
            Guid? toId = TryParseToStatusId(a.PayloadJson);
            if (!toId.HasValue)
                continue;
            if (!issueMap.TryGetValue(a.IssueId, out Issue.Domain.Issue? issue))
                continue;
            if (!wfCache.TryGetValue(issue.WorkflowId, out Workflow.Domain.Workflow? wf))
            {
                wf = await _workflows.GetWithDetailsAsync(issue.WorkflowId, ct);
                wfCache[issue.WorkflowId] = wf;
            }

            Workflow.Domain.WorkflowStatus? st = wf?.Statuses.FirstOrDefault(s => s.Id == toId.Value);
            if (st?.Category != StatusCategory.Done)
                continue;
            if (!issuePoints.TryGetValue(a.IssueId, out decimal pts) || pts <= 0)
                continue;
            if (firstDoneAt[a.IssueId].HasValue)
                continue;
            firstDoneAt[a.IssueId] = a.OccurredAt;
        }

        var days = new List<BurndownDayDto>();
        DateOnly start = DateOnly.FromDateTime(sp.StartDate.UtcDateTime);
        DateOnly end = DateOnly.FromDateTime(chartEnd.UtcDateTime);
        if (end < start)
            end = start;

        double denomDays = (sp.EndDate - sp.StartDate).TotalDays;
        if (denomDays < 1)
            denomDays = 1;

        for (DateOnly d = start; d <= end; d = d.AddDays(1))
        {
            DateTimeOffset dayEnd = new(d.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

            double elapsed = (dayEnd - sp.StartDate).TotalDays;
            if (elapsed < 0)
                elapsed = 0;
            decimal ideal = total * (decimal)(1 - Math.Min(1, elapsed / denomDays));
            if (ideal < 0)
                ideal = 0;

            decimal doneSum = 0;
            foreach (KeyValuePair<Guid, decimal> kv in issuePoints)
            {
                if (kv.Value <= 0)
                    continue;
                if (firstDoneAt[kv.Key] is { } doneTime && doneTime <= dayEnd)
                    doneSum += kv.Value;
            }

            decimal actual = total - doneSum;
            if (actual < 0)
                actual = 0;

            days.Add(new BurndownDayDto(d.ToString("O"), Math.Round(ideal, 2), Math.Round(actual, 2)));
        }

        if (days.Count == 0)
            days.Add(new BurndownDayDto(start.ToString("O"), total, total));

        return Result.Success(new SprintBurndownDto(sprintId, Math.Round(total, 2), days));
    }

    public async Task<Result<VelocityReportDto>> GetVelocityAsync(Guid projectId, int count, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<VelocityReportDto>(ErrorType.Unauthorized, "auth.required");
        if (!await _permissions.HasProjectPermissionAsync(_currentUser.UserId.Value, projectId, PermissionKeys.ProjectView, ct))
            return Result.Failure<VelocityReportDto>(ErrorType.Forbidden, "project.access_denied");

        // Clamp count vào range hợp lý — tránh user query 1 phát toàn bộ history.
        int take = Math.Clamp(count <= 0 ? 6 : count, 1, 50);

        IReadOnlyList<SprintEntity> all = await _sprints.ListByProjectAsync(projectId, ct);
        List<SprintEntity> completed = all
            .Where(s => s.Status == SprintStatus.Completed)
            .OrderByDescending(s => s.EndDate)
            .Take(take)
            .ToList();

        if (completed.Count == 0)
            return Result.Success(new VelocityReportDto(projectId, Array.Empty<SprintVelocityEntryDto>(), 0));

        // Sort tăng dần để chart vẽ trái → phải theo thời gian.
        completed.Reverse();

        // Build per-sprint: committed = sum SprintCommitLine.BurndownPoints,
        //                   completed = sum points của issue có status reach Done category trước EndDate sprint.
        Dictionary<Guid, Workflow.Domain.Workflow?> wfCache = new();
        var entries = new List<SprintVelocityEntryDto>(completed.Count);
        decimal sumCompletedForAvg = 0;
        int countedForAvg = 0;

        foreach (SprintEntity sp in completed)
        {
            IReadOnlyList<SprintCommitLine> lines = await _sprints.ListCommitLinesAsync(sp.Id, ct);
            decimal committed = lines.Sum(x => x.BurndownPoints);

            decimal completedPoints = 0;
            if (committed > 0)
            {
                List<Guid> issueIds = lines.Where(l => l.BurndownPoints > 0).Select(l => l.IssueId).Distinct().ToList();
                Dictionary<Guid, decimal> issuePoints = lines.ToDictionary(x => x.IssueId, x => x.BurndownPoints);

                IReadOnlyList<ActivityEntry> acts = await _activities.ListIssueStatusChangesForIssuesAsync(
                    issueIds, sp.StartDate, sp.EndDate, ct);

                IReadOnlyList<Issue.Domain.Issue> issueEntities =
                    await _issues.ListAsync(i => issueIds.Contains(i.Id), ct);
                Dictionary<Guid, Issue.Domain.Issue> issueMap = issueEntities.ToDictionary(i => i.Id);

                HashSet<Guid> doneIssues = new();
                foreach (ActivityEntry a in acts.OrderBy(x => x.OccurredAt))
                {
                    if (doneIssues.Contains(a.IssueId)) continue;
                    Guid? toId = TryParseToStatusId(a.PayloadJson);
                    if (!toId.HasValue) continue;
                    if (!issueMap.TryGetValue(a.IssueId, out Issue.Domain.Issue? issue)) continue;
                    if (!wfCache.TryGetValue(issue.WorkflowId, out Workflow.Domain.Workflow? wf))
                    {
                        wf = await _workflows.GetWithDetailsAsync(issue.WorkflowId, ct);
                        wfCache[issue.WorkflowId] = wf;
                    }
                    Workflow.Domain.WorkflowStatus? st = wf?.Statuses.FirstOrDefault(s => s.Id == toId.Value);
                    if (st?.Category != StatusCategory.Done) continue;
                    if (a.OccurredAt > sp.EndDate) continue;
                    doneIssues.Add(a.IssueId);
                }

                completedPoints = doneIssues.Sum(id => issuePoints.TryGetValue(id, out decimal p) ? p : 0);

                sumCompletedForAvg += completedPoints;
                countedForAvg++;
            }

            entries.Add(new SprintVelocityEntryDto(
                sp.Id, sp.Name, sp.StartDate, sp.EndDate,
                Math.Round(committed, 2),
                Math.Round(completedPoints, 2)));
        }

        decimal avg = countedForAvg == 0 ? 0 : Math.Round(sumCompletedForAvg / countedForAvg, 2);
        return Result.Success(new VelocityReportDto(projectId, entries, avg));
    }

    private static Guid? TryParseToStatusId(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("toStatusId", out JsonElement prop))
                return null;
            return prop.ValueKind == JsonValueKind.String ? Guid.Parse(prop.GetString()!) : prop.GetGuid();
        }
        catch
        {
            return null;
        }
    }

    private static SprintDto ToDto(SprintEntity sp)
    {
        List<Guid> ordered = sp.Items.OrderBy(x => x.Rank).Select(x => x.IssueId).ToList();
        return new SprintDto(sp.Id, sp.ProjectId, sp.Name, sp.Goal, sp.StartDate, sp.EndDate, (int)sp.Status, ordered);
    }
}
