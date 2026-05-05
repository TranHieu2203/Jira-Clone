using System.Text.Json;
using ActivityLog.Application.Repositories;
using ActivityLog.Domain;
using BB.Common;
using BB.Security;
using Issue.Application;

namespace ActivityLog.Application;

public sealed class ActivityLogService : IActivityLogService
{
    private readonly IActivityEntryRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IIssueAccessGuard _accessGuard;

    public ActivityLogService(IActivityEntryRepository repo, ICurrentUser currentUser, IIssueAccessGuard accessGuard)
    {
        _repo = repo;
        _currentUser = currentUser;
        _accessGuard = accessGuard;
    }

    public async Task<Result<PagedList<ActivityItemDto>>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedList<ActivityItemDto>>(ErrorType.Unauthorized, "auth.required");

        // Cross-project guard: trả 404 thống nhất nếu issue không tồn tại HOẶC user không là member project.
        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure<PagedList<ActivityItemDto>>(ErrorType.NotFound, "issue.not_found");

        var page = await _repo.ListByIssueAsync(issueId, pageIndex, pageSize, ct);
        var items = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<ActivityItemDto>(items, page.TotalCount, page.PageIndex, page.PageSize));
    }

    private static ActivityItemDto ToDto(ActivityEntry e)
    {
        JsonElement? payload = string.IsNullOrWhiteSpace(e.PayloadJson)
            ? null
            : JsonSerializer.Deserialize<JsonElement>(e.PayloadJson);
        return new ActivityItemDto(e.Id, e.IssueId, e.OccurredAt, e.Kind, e.ActorUserId, payload);
    }
}
