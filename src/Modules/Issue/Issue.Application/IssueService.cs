using System.Diagnostics;
using BB.Common;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using BB.Security;
using CustomField.Application;
using Issue.Application.Repositories;
using Issue.Domain;
using Microsoft.Extensions.Logging;
using Project.Application;
using Workflow.Application;
using Workflow.Application.Engine;

namespace Issue.Application;

public sealed class IssueService : IIssueService
{
    private readonly IIssueRepository _repo;
    private readonly IIssueUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIssueNumberAllocator _allocator;
    private readonly IIssueTypeReader _issueTypeReader;
    private readonly IWorkflowResolver _workflowResolver;
    private readonly IWorkflowProvisioner _workflowProvisioner;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IIssueFieldValueService _fieldValueService;
    private readonly IIssueFieldValueIssueFilter _fieldIssueFilter;
    private readonly IEventBus _eventBus;
    private readonly ILogger<IssueService> _logger;

    public IssueService(
        IIssueRepository repo,
        IIssueUnitOfWork uow,
        ICurrentUser currentUser,
        IIssueNumberAllocator allocator,
        IIssueTypeReader issueTypeReader,
        IWorkflowResolver workflowResolver,
        IWorkflowProvisioner workflowProvisioner,
        IWorkflowEngine workflowEngine,
        IIssueFieldValueService fieldValueService,
        IIssueFieldValueIssueFilter fieldIssueFilter,
        IEventBus eventBus,
        ILogger<IssueService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _allocator = allocator;
        _issueTypeReader = issueTypeReader;
        _workflowResolver = workflowResolver;
        _workflowProvisioner = workflowProvisioner;
        _workflowEngine = workflowEngine;
        _fieldValueService = fieldValueService;
        _fieldIssueFilter = fieldIssueFilter;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<IssueDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var i = await _repo.GetWithWatchersAsync(id, ct);
        return i is null
            ? Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found")
            : Result.Success(Mappers.ToDto(i));
    }

    public async Task<Result<IssueDto>> GetByKeyAsync(string issueKey, CancellationToken ct = default)
    {
        var i = await _repo.GetByKeyAsync(issueKey.ToUpperInvariant(), ct);
        return i is null
            ? Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found")
            : Result.Success(Mappers.ToDto(i));
    }

    public async Task<Result<PagedList<IssueSummaryDto>>> SearchAsync(SearchIssuesRequest request, CancellationToken ct = default)
    {
        Result<JqlLiteResult> jql = JqlLiteParser.Parse(request.Jql, _currentUser.UserId);
        if (!jql.IsSuccess || jql.Data is null)
        {
            return Result.Failure<PagedList<IssueSummaryDto>>(
                jql.ErrorType,
                jql.MessageKey ?? "issue.search.jql.invalid",
                jql.Errors);
        }

        Guid? assigneeId = request.AssigneeId;
        bool assigneeUnassigned = false;
        Guid? statusId = request.CurrentStatusId;
        string? textSearch = request.TextSearch;

        if (jql.Data.HasAssigneeClause)
        {
            assigneeUnassigned = jql.Data.AssigneeUnassignedOnly;
            assigneeId = jql.Data.AssigneeUnassignedOnly ? null : jql.Data.AssigneeId;
        }

        if (jql.Data.HasStatusClause)
            statusId = jql.Data.StatusId;

        if (jql.Data.HasTextClause)
            textSearch = jql.Data.TextContains;

        if (assigneeId.HasValue && assigneeUnassigned)
        {
            return Result.Failure<PagedList<IssueSummaryDto>>(
                ErrorType.Validation,
                "issue.search.conflicting_assignee_filters");
        }

        IReadOnlySet<Guid>? restrict = null;
        if (request.FieldFilters is { Count: > 0 })
        {
            List<IssueFieldIndexedCriterion> fc = request.FieldFilters
                .Select(f => new IssueFieldIndexedCriterion(
                    f.CustomFieldId,
                    f.IndexedStringEquals,
                    f.IndexedNumberEquals,
                    f.IndexedDateEquals))
                .ToList();

            restrict = await _fieldIssueFilter.MatchingIssueIdsAsync(fc, ct);
            if (restrict is not null && restrict.Count == 0)
            {
                int p = Math.Max(request.PageIndex, 1);
                int s = Math.Max(request.PageSize, 1);
                var empty = new PagedList<IssueSummaryDto>(new List<IssueSummaryDto>(), 0, p, s);
                return Result.Success(empty);
            }
        }

        var criteria = new IssueSearchCriteria(
            request.ProjectId, request.IssueTypeId, assigneeId, request.ReporterId,
            statusId, request.Priority, textSearch, request.IncludeArchived,
            request.PageIndex, request.PageSize, request.Sort,
            assigneeUnassigned,
            restrict);

        var page = await _repo.SearchAsync(criteria, ct);
        var dtos = page.Items.Select(Mappers.ToSummary).ToList();
        return Result.Success(new PagedList<IssueSummaryDto>(dtos, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<IReadOnlyList<IssueSummaryDto>>> ListChildrenAsync(Guid parentIssueId, CancellationToken ct = default)
    {
        var list = await _repo.ListByParentAsync(parentIssueId, ct);
        return Result.Success<IReadOnlyList<IssueSummaryDto>>(list.Select(Mappers.ToSummary).ToList());
    }

    public async Task<Result<IssueDto>> CreateAsync(CreateIssueRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IssueDto>(ErrorType.Unauthorized, "auth.required");

        // 1. Validate issueType belongs to project
        if (!await _issueTypeReader.ExistsInProjectAsync(request.ProjectId, request.IssueTypeId, ct))
            return Result.Failure<IssueDto>(
                ErrorType.Validation, "issue.issue_type.invalid",
                new[] { new ResultError("ISSUE_TYPE_INVALID", "issue.issue_type.invalid", Field: "issueTypeId") });

        // 2. Resolve workflow + initial status. Lazy-provision nếu project chưa có scheme
        // (xảy ra với project mới — provisioner clone template SOFTWARE_SIMPLE).
        var workflowResult = await _workflowResolver.ResolveForIssueAsync(request.ProjectId, request.IssueTypeId, ct);
        if (!workflowResult.IsSuccess && workflowResult.MessageKey == "workflow.scheme.not_found")
        {
            var provisionResult = await _workflowProvisioner.EnsureForProjectAsync(request.ProjectId, ct);
            if (!provisionResult.IsSuccess)
                return Result.Failure<IssueDto>(provisionResult.ErrorType, provisionResult.MessageKey ?? "workflow.provision.failed");

            workflowResult = await _workflowResolver.ResolveForIssueAsync(request.ProjectId, request.IssueTypeId, ct);
        }
        if (!workflowResult.IsSuccess || workflowResult.Data is null)
            return Result.Failure<IssueDto>(workflowResult.ErrorType, workflowResult.MessageKey ?? "workflow.not_found", workflowResult.Errors);

        // 3. Allocate issue number — atomic via Project module
        var allocResult = await _allocator.AllocateAsync(request.ProjectId, ct);
        if (!allocResult.IsSuccess || allocResult.Data is null)
            return Result.Failure<IssueDto>(allocResult.ErrorType, allocResult.MessageKey ?? "issue.allocate_number.failed");

        var alloc = allocResult.Data;
        var workflow = workflowResult.Data;

        // 4. Create issue aggregate
        var issue = new Domain.Issue(
            request.ProjectId,
            alloc.IssueKey,
            alloc.Number,
            request.IssueTypeId,
            workflow.WorkflowId,
            workflow.InitialStatusId,
            request.Summary,
            _currentUser.UserId.Value,
            request.Description,
            (Priority)(request.Priority ?? (int)Priority.Medium),
            request.ParentIssueId,
            request.AssigneeId,
            request.DueDate,
            request.StoryPoints,
            request.Labels);

        await _repo.AddAsync(issue, ct);
        await _uow.SaveChangesAsync(ct);

        // 5. Set custom field values (riêng transaction trong CustomField context — chấp nhận eventual consistency)
        if (request.CustomFieldValues is { Count: > 0 })
        {
            var setReq = new SetIssueFieldValuesRequest(
                issue.Id, request.ProjectId, request.IssueTypeId,
                request.CustomFieldValues.Select(kv => new SetIssueFieldValueRequest(kv.Key, kv.Value)).ToList());
            var fieldResult = await _fieldValueService.SetValuesAsync(setReq, ct);
            if (!fieldResult.IsSuccess)
            {
                _logger.LogWarning("Issue {Key} created but custom field values failed: {Errors}",
                    issue.Key, string.Join(",", fieldResult.Errors.Select(e => e.Code)));
                // Không rollback — caller có thể retry SetValues riêng. Trả lỗi để FE biết.
                return Result.Failure<IssueDto>(ErrorType.Validation, "issue.created.field_values_failed", fieldResult.Errors);
            }
        }

        await PublishAssigneeChangedIfNeededAsync(issue, null, issue.AssigneeId, ct);

        _logger.LogInformation("Issue created Key={Key} Project={ProjectId}", issue.Key, issue.ProjectId);
        return Result.Success(Mappers.ToDto(issue), "issue.created.success", new { key = issue.Key });
    }

    public async Task<Result<IssueDto>> UpdateAsync(Guid id, UpdateIssueRequest request, CancellationToken ct = default)
    {
        var issue = await _repo.GetWithWatchersAsync(id, ct);
        if (issue is null) return Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found");

        Guid? prevAssignee = issue.AssigneeId;

        issue.UpdateSummary(request.Summary);
        issue.UpdateDescription(request.Description);
        if (request.Priority.HasValue) issue.ChangePriority((Priority)request.Priority.Value);
        issue.Assign(request.AssigneeId);
        issue.SetParent(request.ParentIssueId);
        issue.SetDueDate(request.DueDate);
        issue.SetStoryPoints(request.StoryPoints);
        issue.SetLabels(request.Labels);

        if (request.OriginalEstimateMinutes.HasValue || request.RemainingEstimateMinutes.HasValue || request.TimeSpentMinutes.HasValue)
            issue.SetTimeTracking(request.OriginalEstimateMinutes, request.RemainingEstimateMinutes, request.TimeSpentMinutes);

        _repo.Update(issue);
        await _uow.SaveChangesAsync(ct);

        await PublishAssigneeChangedIfNeededAsync(issue, prevAssignee, issue.AssigneeId, ct);

        return Result.Success(Mappers.ToDto(issue), "issue.updated.success");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var issue = await _repo.GetByIdAsync(id, ct);
        if (issue is null) return Result.Failure(ErrorType.NotFound, "issue.not_found");
        _repo.Remove(issue);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "issue.deleted.success");
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var issue = await _repo.GetByIdAsync(id, ct);
        if (issue is null) return Result.Failure(ErrorType.NotFound, "issue.not_found");
        issue.Archive();
        _repo.Update(issue); await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "issue.archived");
    }

    public async Task<Result> UnarchiveAsync(Guid id, CancellationToken ct = default)
    {
        var issue = await _repo.GetByIdAsync(id, ct);
        if (issue is null) return Result.Failure(ErrorType.NotFound, "issue.not_found");
        issue.Unarchive();
        _repo.Update(issue); await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "issue.unarchived");
    }

    public async Task<Result<IssueDto>> TransitionAsync(Guid id, TransitionIssueRequest request, CancellationToken ct = default)
    {
        var issue = await _repo.GetWithWatchersAsync(id, ct);
        if (issue is null) return Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found");

        Guid fromStatus = issue.CurrentStatusId;
        Guid? prevAssignee = issue.AssigneeId;

        // Engine validates + executes rules/validators/post-functions, persists history.
        var outcome = await _workflowEngine.TransitionAsync(
            issue.Id, issue.ProjectId, issue.IssueTypeId, issue.CurrentStatusId,
            request.TransitionId, request.Inputs, request.Comment, ct);

        if (!outcome.IsSuccess || outcome.Data is null)
            return Result.Failure<IssueDto>(outcome.ErrorType, outcome.MessageKey ?? "workflow.transition.failed", outcome.Errors);

        // Apply status change to Issue aggregate (Issue's own DB context).
        issue.TransitionTo(outcome.Data.ToStatusId, outcome.Data.TransitionId);

        // Apply field changes from post-functions targeted at core fields.
        // (Custom field changes are out of scope — engine routes them via own service in future work.)
        ApplyCoreFieldChanges(issue, outcome.Data.FieldChanges);

        _repo.Update(issue);
        await _uow.SaveChangesAsync(ct);

        List<Guid> watchers = WatcherUserIds(issue);

        if (fromStatus != issue.CurrentStatusId)
        {
            await _eventBus.PublishAsync(new IssueStatusChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                IntegrationTraceId(),
                issue.Id,
                issue.Key,
                issue.ProjectId,
                fromStatus,
                issue.CurrentStatusId,
                _currentUser.UserId,
                watchers), ct);
        }

        await PublishAssigneeChangedIfNeededAsync(issue, prevAssignee, issue.AssigneeId, ct);

        return Result.Success(Mappers.ToDto(issue), "issue.transitioned",
            new { from = outcome.Data.FromStatusId, to = outcome.Data.ToStatusId });
    }

    private static void ApplyCoreFieldChanges(Domain.Issue issue, IReadOnlyDictionary<string, System.Text.Json.JsonElement> changes)
    {
        if (changes.TryGetValue("assignee", out var assigneeJson))
        {
            if (assigneeJson.ValueKind == System.Text.Json.JsonValueKind.Null) issue.Assign(null);
            else if (Guid.TryParse(assigneeJson.GetString(), out var newAssignee)) issue.Assign(newAssignee);
        }

        if (changes.TryGetValue("priority", out var priorityJson) &&
            priorityJson.ValueKind == System.Text.Json.JsonValueKind.Number &&
            Enum.IsDefined(typeof(Priority), priorityJson.GetInt32()))
        {
            issue.ChangePriority((Priority)priorityJson.GetInt32());
        }
    }

    public async Task<Result<IssueDto>> AddWatcherAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var issue = await _repo.GetWithWatchersAsync(id, ct);
        if (issue is null) return Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found");
        issue.AddWatcher(userId);
        _repo.Update(issue); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(issue), "issue.watcher.added");
    }

    public async Task<Result<IssueDto>> RemoveWatcherAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var issue = await _repo.GetWithWatchersAsync(id, ct);
        if (issue is null) return Result.Failure<IssueDto>(ErrorType.NotFound, "issue.not_found");
        issue.RemoveWatcher(userId);
        _repo.Update(issue); await _uow.SaveChangesAsync(ct);
        return Result.Success(Mappers.ToDto(issue), "issue.watcher.removed");
    }

    private static string? IntegrationTraceId() =>
        Activity.Current?.TraceId.ToString();

    private static List<Guid> WatcherUserIds(Domain.Issue issue) =>
        issue.Watchers.Select(w => w.UserId).Distinct().ToList();

    private async Task PublishAssigneeChangedIfNeededAsync(
        Domain.Issue issue,
        Guid? previousAssignee,
        Guid? newAssignee,
        CancellationToken ct)
    {
        if (newAssignee == previousAssignee || newAssignee is null)
            return;

        if (_currentUser.UserId is null)
            return;

        var evt = new IssueAssigneeChangedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            IntegrationTraceId(),
            issue.Id,
            issue.Key,
            issue.ProjectId,
            previousAssignee,
            newAssignee,
            _currentUser.UserId,
            WatcherUserIds(issue));

        await _eventBus.PublishAsync(evt, ct);
    }
}
