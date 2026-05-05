using System.Diagnostics;
using BB.Common;
using BB.EventBus;
using BB.EventBus.IntegrationEvents;
using BB.Security;
using Comment.Application.Repositories;
using Comment.Domain;
using Identity.Application;
using Issue.Application;
using Issue.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Comment.Application;

public sealed class CommentService : ICommentService
{
    private readonly ICommentRepository _repo;
    private readonly ICommentUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIssueNotificationSnapshotReader _issueSnapshot;
    private readonly IUserNameLookup _userNames;
    private readonly IIssueRealtimeNotifier _realtime;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ICommentRepository repo,
        ICommentUnitOfWork uow,
        ICurrentUser currentUser,
        IIssueNotificationSnapshotReader issueSnapshot,
        IUserNameLookup userNames,
        IIssueRealtimeNotifier realtime,
        IEventBus eventBus,
        ILogger<CommentService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _issueSnapshot = issueSnapshot;
        _userNames = userNames;
        _realtime = realtime;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<PagedList<CommentDto>>> ListByIssueAsync(Guid issueId, int pageIndex = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var page = await _repo.ListByIssueAsync(issueId, pageIndex, pageSize, ct);
        var dtos = page.Items.Select(Mappers.ToDto).ToList();
        return Result.Success(new PagedList<CommentDto>(dtos, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<CommentDto>> CreateAsync(CreateCommentRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CommentDto>(ErrorType.Unauthorized, "auth.required");

        var c = new Comment.Domain.Comment(request.IssueId, _currentUser.UserId.Value, request.Body);
        await _repo.AddAsync(c, ct);
        await _uow.SaveChangesAsync(ct);

        await PublishCommentAddedAsync(c, ct);

        _logger.LogInformation("Comment added Id={Id} Issue={IssueId} Author={Author}",
            c.Id, c.IssueId, c.AuthorId);
        return Result.Success(Mappers.ToDto(c), "comment.added");
    }

    public async Task<Result<CommentDto>> UpdateAsync(Guid id, UpdateCommentRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CommentDto>(ErrorType.Unauthorized, "auth.required");

        var c = await _repo.GetByIdAsync(id, ct);
        if (c is null) return Result.Failure<CommentDto>(ErrorType.NotFound, CommentErrors.MsgNotFound);

        try { c.Edit(_currentUser.UserId.Value, request.Body); }
        catch (DomainException dex) when (dex.Code == CommentErrors.NotAuthor)
        {
            return Result.Failure<CommentDto>(ErrorType.Forbidden, dex.MessageKey);
        }

        _repo.Update(c);
        await _uow.SaveChangesAsync(ct);

        IssueNotificationSnapshot? snapU = await _issueSnapshot.GetAsync(c.IssueId, ct);
        if (snapU is not null)
        {
            await _realtime.NotifyProjectBoardAsync(
                snapU.ProjectId,
                new IssueBoardRealtimeEvent("comment", snapU.IssueId, snapU.IssueKey),
                ct);
            await _realtime.NotifyIssueThreadAsync(
                c.IssueId,
                new IssueThreadRealtimeEvent("comment_updated", c.Id),
                ct);
        }

        return Result.Success(Mappers.ToDto(c), "comment.updated");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        var c = await _repo.GetByIdAsync(id, ct);
        if (c is null) return Result.Failure(ErrorType.NotFound, CommentErrors.MsgNotFound);

        try { c.EnsureCanDelete(_currentUser.UserId.Value); }
        catch (DomainException dex) when (dex.Code == CommentErrors.NotAuthor)
        {
            return Result.Failure(ErrorType.Forbidden, dex.MessageKey);
        }

        Guid issueId = c.IssueId;
        Guid commentId = c.Id;

        c.RaiseDeletedEvent(_currentUser.UserId.Value);
        _repo.Remove(c);
        await _uow.SaveChangesAsync(ct);

        IssueNotificationSnapshot? snapD = await _issueSnapshot.GetAsync(issueId, ct);
        if (snapD is not null)
        {
            await _realtime.NotifyProjectBoardAsync(
                snapD.ProjectId,
                new IssueBoardRealtimeEvent("comment", snapD.IssueId, snapD.IssueKey),
                ct);
            await _realtime.NotifyIssueThreadAsync(
                issueId,
                new IssueThreadRealtimeEvent("comment_deleted", commentId),
                ct);
        }

        return Result.Success(messageKey: "comment.deleted");
    }

    private async Task PublishCommentAddedAsync(Comment.Domain.Comment c, CancellationToken ct)
    {
        IssueNotificationSnapshot? snap = await _issueSnapshot.GetAsync(c.IssueId, ct);
        if (snap is null)
            return;

        List<Guid> mentionIds = new();
        foreach (string userName in c.Mentions)
        {
            Guid? id = await _userNames.FindActiveUserIdByUserNameAsync(userName, ct);
            if (id is not null)
                mentionIds.Add(id.Value);
        }

        string preview = c.Body.Length <= 200 ? c.Body : c.Body[..200];

        var evt = new CommentAddedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Activity.Current?.TraceId.ToString(),
            snap.IssueId,
            snap.IssueKey,
            snap.ProjectId,
            snap.AssigneeId,
            c.Id,
            c.AuthorId,
            preview,
            mentionIds,
            snap.WatcherUserIds.ToList());

        await _eventBus.PublishAsync(evt, ct);

        await _realtime.NotifyProjectBoardAsync(
            snap.ProjectId,
            new IssueBoardRealtimeEvent("comment", snap.IssueId, snap.IssueKey),
            ct);
        await _realtime.NotifyIssueThreadAsync(
            snap.IssueId,
            new IssueThreadRealtimeEvent("comment", c.Id),
            ct);
    }
}
