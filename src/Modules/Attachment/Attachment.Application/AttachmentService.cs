using Attachment.Application.Repositories;
using Attachment.Domain;
using BB.Common;
using BB.Security;
using BB.Storage;
using Issue.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Attachment.Application;

public sealed class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _repo;
    private readonly IAttachmentUnitOfWork _uow;
    private readonly IIssueAccessGuard _accessGuard;
    private readonly IPermissionChecker _permissions;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IIssueRealtimeNotifier _realtime;
    private readonly StorageOptions _storageOpts;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IAttachmentRepository repo,
        IAttachmentUnitOfWork uow,
        IIssueAccessGuard accessGuard,
        IPermissionChecker permissions,
        IFileStorage storage,
        ICurrentUser currentUser,
        IIssueRealtimeNotifier realtime,
        IOptions<StorageOptions> storageOpts,
        ILogger<AttachmentService> logger)
    {
        _repo = repo;
        _uow = uow;
        _accessGuard = accessGuard;
        _permissions = permissions;
        _storage = storage;
        _currentUser = currentUser;
        _realtime = realtime;
        _storageOpts = storageOpts.Value;
        _logger = logger;
    }

    public async Task<Result<PagedList<AttachmentDto>>> ListByIssueAsync(
        Guid issueId,
        int pageIndex = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedList<AttachmentDto>>(ErrorType.Unauthorized, "auth.required");

        // Cross-project guard: trả `IssueMissing` thống nhất khi issue không tồn tại HOẶC user không là member project.
        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure<PagedList<AttachmentDto>>(ErrorType.NotFound, AttachmentErrors.IssueMissing);

        PagedList<IssueAttachment> page = await _repo.ListByIssueAsync(issueId, pageIndex, pageSize, ct);
        List<AttachmentDto> dtos = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<AttachmentDto>(dtos, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<AttachmentDto>> UploadAsync(
        Guid issueId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<AttachmentDto>(ErrorType.Unauthorized, "auth.required");

        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure<AttachmentDto>(ErrorType.NotFound, AttachmentErrors.IssueMissing);

        // R2: upload attachment = edit issue. Viewer không được upload.
        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, access.ProjectId, PermissionKeys.IssueEdit, ct);
        if (perm.IsFailure) return Result.Failure<AttachmentDto>(perm);

        if (sizeBytes > _storageOpts.MaxUploadBytes)
            return Result.Failure<AttachmentDto>(ErrorType.Validation, AttachmentErrors.TooLarge);

        string safeName = SanitizeFileName(fileName);
        if (string.IsNullOrEmpty(safeName))
            return Result.Failure<AttachmentDto>(ErrorType.Validation, AttachmentErrors.InvalidFileName);

        Guid attachmentId = Guid.NewGuid();
        string storageKey = BuildStorageKey(issueId, attachmentId, safeName);

        await _storage.PutAsync(storageKey, content, contentType, ct);
        try
        {
            IssueAttachment entity = new(
                issueId,
                _currentUser.UserId.Value,
                safeName,
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                sizeBytes,
                storageKey);
            entity.Id = attachmentId;

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // F12: realtime — clients đang mở issue/board sẽ reload danh sách attachment.
            await _realtime.NotifyProjectBoardAsync(
                access.ProjectId,
                new IssueBoardRealtimeEvent("attachment", issueId, string.Empty),
                ct);
            await _realtime.NotifyIssueThreadAsync(issueId, new IssueThreadRealtimeEvent("attachment"), ct);

            _logger.LogInformation("Attachment {AttachmentId} saved for issue {IssueId}", attachmentId, issueId);
            return Result.Success(ToDto(entity), messageKey: "attachment.uploaded");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rolling back storage after DB failure for {Key}", storageKey);
            await _storage.DeleteAsync(storageKey, ct);
            throw;
        }
    }

    public async Task<Result<AttachmentDownload>> OpenDownloadAsync(
        Guid issueId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<AttachmentDownload>(ErrorType.Unauthorized, "auth.required");

        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure<AttachmentDownload>(ErrorType.NotFound, AttachmentErrors.IssueMissing);

        IssueAttachment? a = await _repo.GetByIdAndIssueAsync(issueId, attachmentId, ct);
        if (a is null)
            return Result.Failure<AttachmentDownload>(ErrorType.NotFound, AttachmentErrors.NotFound);

        Stream? stream = await _storage.OpenReadAsync(a.StorageKey, ct);
        if (stream is null)
            return Result.Failure<AttachmentDownload>(ErrorType.NotFound, AttachmentErrors.NotFound);

        return Result.Success(new AttachmentDownload(stream, a.ContentType, a.FileName));
    }

    public async Task<Result> DeleteAsync(Guid issueId, Guid attachmentId, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        IssueAccessSnapshot? access = await _accessGuard.ResolveAccessAsync(_currentUser.UserId.Value, issueId, ct);
        if (access is null)
            return Result.Failure(ErrorType.NotFound, AttachmentErrors.IssueMissing);

        // R2: delete attachment = edit issue (Viewer không được dù là người upload trước đó).
        var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, access.ProjectId, PermissionKeys.IssueEdit, ct);
        if (perm.IsFailure) return perm;

        IssueAttachment? a = await _repo.GetByIdAndIssueAsync(issueId, attachmentId, ct);
        if (a is null)
            return Result.Failure(ErrorType.NotFound, AttachmentErrors.NotFound);

        if (a.UploadedByUserId != _currentUser.UserId.Value)
            return Result.Failure(ErrorType.Forbidden, AttachmentErrors.ForbiddenDelete);

        string key = a.StorageKey;
        _repo.Remove(a);
        await _uow.SaveChangesAsync(ct);

        try
        {
            await _storage.DeleteAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage delete failed for {Key} (metadata removed)", key);
        }

        // F12: realtime — báo client list issue & detail reload.
        await _realtime.NotifyProjectBoardAsync(
            access.ProjectId,
            new IssueBoardRealtimeEvent("attachment", issueId, string.Empty),
            ct);
        await _realtime.NotifyIssueThreadAsync(issueId, new IssueThreadRealtimeEvent("attachment"), ct);

        return Result.Success(messageKey: "attachment.deleted");
    }

    private static AttachmentDto ToDto(IssueAttachment a) =>
        new(a.Id, a.IssueId, a.UploadedByUserId, a.FileName, a.ContentType, a.SizeBytes, a.CreatedAt);

    private static string SanitizeFileName(string fileName)
    {
        string name = Path.GetFileName(fileName.Trim());
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    private static string BuildStorageKey(Guid issueId, Guid attachmentId, string safeName) =>
        $"issues/{issueId:N}/{attachmentId:N}/{safeName}";
}
