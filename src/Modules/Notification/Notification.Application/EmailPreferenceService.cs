using BB.Common;
using BB.Security;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application;

public sealed class EmailPreferenceService : IEmailPreferenceService
{
    private readonly IEmailUserPreferenceRepository _repo;
    private readonly INotificationUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public EmailPreferenceService(
        IEmailUserPreferenceRepository repo,
        INotificationUnitOfWork uow,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<EmailUserPreferenceDto>> GetMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<EmailUserPreferenceDto>(ErrorType.Unauthorized, "auth.required");

        Guid userId = _currentUser.UserId.Value;
        EmailUserPreference? p = await _repo.FindByUserIdAsync(userId, ct);
        if (p is null)
        {
            // Auto-create default (no flags set) khi user truy cập lần đầu — UX mượt.
            p = new EmailUserPreference(userId);
            await _repo.AddAsync(p, ct);
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success(ToDto(p));
    }

    public async Task<Result<EmailUserPreferenceDto>> UpdateMineAsync(UpdateEmailPreferenceRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<EmailUserPreferenceDto>(ErrorType.Unauthorized, "auth.required");

        Guid userId = _currentUser.UserId.Value;
        EmailUserPreference? p = await _repo.FindByUserIdAsync(userId, ct);
        if (p is null)
        {
            p = new EmailUserPreference(userId);
            p.Update(request.NoAssignee, request.NoStatus, request.NoComment, request.NoMention);
            await _repo.AddAsync(p, ct);
        }
        else
        {
            p.Update(request.NoAssignee, request.NoStatus, request.NoComment, request.NoMention);
            _repo.Update(p);
        }
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(p), "email_preference.updated");
    }

    private static EmailUserPreferenceDto ToDto(EmailUserPreference p) =>
        new(p.UserId, p.NoAssignee, p.NoStatus, p.NoComment, p.NoMention);
}
