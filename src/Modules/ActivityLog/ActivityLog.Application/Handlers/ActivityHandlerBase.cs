using ActivityLog.Application.Repositories;
using ActivityLog.Domain;
using BB.Security;

namespace ActivityLog.Application.Handlers;

public abstract class ActivityHandlerBase
{
    protected readonly IActivityEntryRepository Repo;
    protected readonly IActivityLogUnitOfWork Uow;
    protected readonly ICurrentUser CurrentUser;

    protected ActivityHandlerBase(IActivityEntryRepository repo, IActivityLogUnitOfWork uow, ICurrentUser currentUser)
    {
        Repo = repo;
        Uow = uow;
        CurrentUser = currentUser;
    }

    protected Guid? ResolveActor(Guid? fallbackFromEvent) => CurrentUser.UserId ?? fallbackFromEvent;

    protected async Task AppendAsync(Guid issueId, DateTimeOffset occurredAt, string kind, Guid? actorUserId, object? payload, CancellationToken ct)
    {
        var json = ActivityPayload.Serialize(payload);
        await Repo.AddAsync(new ActivityEntry(issueId, occurredAt, kind, actorUserId, json), ct);
        await Uow.SaveChangesAsync(ct);
    }
}
