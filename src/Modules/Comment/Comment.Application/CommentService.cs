using BB.Common;
using BB.Security;
using Comment.Application.Repositories;
using Comment.Domain;
using Microsoft.Extensions.Logging;

namespace Comment.Application;

public sealed class CommentService : ICommentService
{
    private readonly ICommentRepository _repo;
    private readonly ICommentUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CommentService> _logger;

    public CommentService(ICommentRepository repo, ICommentUnitOfWork uow, ICurrentUser currentUser, ILogger<CommentService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
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

        var c = new Domain.Comment(request.IssueId, _currentUser.UserId.Value, request.Body);
        await _repo.AddAsync(c, ct);
        await _uow.SaveChangesAsync(ct);

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

        c.RaiseDeletedEvent(_currentUser.UserId.Value);
        _repo.Remove(c);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "comment.deleted");
    }
}
