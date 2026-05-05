using BB.Common;

namespace Notification.Application;

public interface IEmailService
{
    Task<Result<EmailTemplateDto>> UpsertTemplateAsync(UpsertEmailTemplateRequest request, CancellationToken ct = default);
    Task<Result<PagedList<EmailTemplateDto>>> ListTemplatesAsync(int pageIndex, int pageSize, string? q, CancellationToken ct = default);
    Task<Result<EmailTemplateDto>> GetTemplateAsync(string key, CancellationToken ct = default);

    Task<Result<EmailLogDto>> SendAsync(SendEmailRequest request, CancellationToken ct = default);
    Task<Result<PagedList<EmailLogDto>>> ListLogsAsync(int pageIndex, int pageSize, string? templateKey, string? toEmail, string? status, CancellationToken ct = default);

    /// <summary>
    /// R6 DLQ: retry 1 email log đã Failed. Re-render từ template + args trong log,
    /// tạo log entry mới với status mới (Sent/Failed); log gốc giữ nguyên cho audit.
    /// </summary>
    Task<Result<EmailLogDto>> RetryAsync(Guid logId, CancellationToken ct = default);
}

