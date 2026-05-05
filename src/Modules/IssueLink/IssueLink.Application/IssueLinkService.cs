using BB.Common;
using BB.Security;
using IssueLink.Application.Repositories;
using IssueLink.Domain;
using Issue.Application;
using Microsoft.Extensions.Logging;

namespace IssueLink.Application;

public sealed class IssueLinkService : IIssueLinkService
{
    private readonly IIssueLinkRepository _repo;
    private readonly IIssueLinkUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IIssueAccessGuard _accessGuard;
    private readonly IPermissionChecker _permissions;
    private readonly ILogger<IssueLinkService> _logger;

    public IssueLinkService(
        IIssueLinkRepository repo,
        IIssueLinkUnitOfWork uow,
        ICurrentUser currentUser,
        IIssueAccessGuard accessGuard,
        IPermissionChecker permissions,
        ILogger<IssueLinkService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _accessGuard = accessGuard;
        _permissions = permissions;
        _logger = logger;
    }

    public async Task<Result<IssueLinksForIssueDto>> ListByIssueAsync(Guid issueId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IssueLinksForIssueDto>(ErrorType.Unauthorized, "auth.required");

        // Cross-project guard cho issue đang xem (không leak cross-project tồn tại link).
        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure<IssueLinksForIssueDto>(ErrorType.NotFound, "issue.not_found");

        var outgoing = await _repo.ListBySourceAsync(issueId, ct);
        var incoming = await _repo.ListByTargetAsync(issueId, ct);

        return Result.Success(new IssueLinksForIssueDto(
            issueId,
            outgoing.Select(ToDto).ToList(),
            incoming.Select(ToDto).ToList()));
    }

    public async Task<Result<IssueLinkDto>> CreateAsync(CreateIssueLinkRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IssueLinkDto>(ErrorType.Unauthorized, "auth.required");

        if (request.SourceIssueId == request.TargetIssueId)
            return Result.Failure<IssueLinkDto>(ErrorType.Validation, IssueLinkErrors.MsgSelfLink);

        // Source issue: phải là member project + có quyền edit (link tạo từ phía source).
        IssueAccessSnapshot? sourceAccess = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, request.SourceIssueId, ct);
        if (sourceAccess is null)
            return Result.Failure<IssueLinkDto>(ErrorType.NotFound, IssueLinkErrors.MsgSourceMissing);

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, sourceAccess.ProjectId, PermissionKeys.IssueEdit, ct);
        if (perm.IsFailure) return Result.Failure<IssueLinkDto>(perm);

        // Target issue: phải là member project (không leak tồn tại issue private cross-workspace).
        IssueAccessSnapshot? targetAccess = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, request.TargetIssueId, ct);
        if (targetAccess is null)
            return Result.Failure<IssueLinkDto>(ErrorType.NotFound, IssueLinkErrors.MsgTargetMissing);

        // Idempotent: cùng cặp source/target/type → chỉ 1 row.
        if (await _repo.ExistsAsync(request.SourceIssueId, request.TargetIssueId, request.LinkType, ct))
            return Result.Failure<IssueLinkDto>(ErrorType.Conflict, IssueLinkErrors.MsgDuplicateLink);

        Domain.IssueLink link;
        try
        {
            link = new Domain.IssueLink(
                request.SourceIssueId, sourceAccess.ProjectId,
                request.TargetIssueId, targetAccess.ProjectId,
                request.LinkType, _currentUser.UserId.Value);
        }
        catch (DomainException dex)
        {
            return Result.Failure<IssueLinkDto>(ErrorType.Validation, dex.MessageKey);
        }

        await _repo.AddAsync(link, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("IssueLink added Id={Id} {Source} -[{Type}]-> {Target}",
            link.Id, link.SourceIssueId, link.LinkType, link.TargetIssueId);

        return Result.Success(ToDto(link), "issue_link.added");
    }

    public async Task<Result> DeleteAsync(Guid linkId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        var link = await _repo.GetByIdAsync(linkId, ct);
        if (link is null)
            return Result.Failure(ErrorType.NotFound, IssueLinkErrors.MsgNotFound);

        // Quyền xoá: là member project source AND có IssueEdit.
        IssueAccessSnapshot? sourceAccess = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, link.SourceIssueId, ct);
        if (sourceAccess is null)
            return Result.Failure(ErrorType.NotFound, IssueLinkErrors.MsgNotFound);

        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, sourceAccess.ProjectId, PermissionKeys.IssueEdit, ct);
        if (perm.IsFailure) return perm;

        link.RaiseRemovedEvent(_currentUser.UserId.Value);
        _repo.Remove(link);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(messageKey: "issue_link.removed");
    }

    private static IssueLinkDto ToDto(Domain.IssueLink l) =>
        new(l.Id, l.SourceIssueId, l.TargetIssueId, l.LinkType, l.LinkType.ForwardKey(), l.CreatedAt);
}
