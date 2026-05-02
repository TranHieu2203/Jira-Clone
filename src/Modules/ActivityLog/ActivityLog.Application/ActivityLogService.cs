using System.Text.Json;
using ActivityLog.Application.Repositories;
using ActivityLog.Domain;
using BB.Common;

namespace ActivityLog.Application;

public sealed class ActivityLogService : IActivityLogService
{
    private readonly IActivityEntryRepository _repo;

    public ActivityLogService(IActivityEntryRepository repo) => _repo = repo;

    public async Task<Result<PagedList<ActivityItemDto>>> ListByIssueAsync(Guid issueId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
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
