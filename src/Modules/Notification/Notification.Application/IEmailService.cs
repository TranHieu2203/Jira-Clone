using BB.Common;

namespace Notification.Application;

public interface IEmailService
{
    Task<Result<EmailTemplateDto>> UpsertTemplateAsync(UpsertEmailTemplateRequest request, CancellationToken ct = default);
    Task<Result<PagedList<EmailTemplateDto>>> ListTemplatesAsync(int pageIndex, int pageSize, string? q, CancellationToken ct = default);
    Task<Result<EmailTemplateDto>> GetTemplateAsync(string key, CancellationToken ct = default);

    Task<Result<EmailLogDto>> SendAsync(SendEmailRequest request, CancellationToken ct = default);
    Task<Result<PagedList<EmailLogDto>>> ListLogsAsync(int pageIndex, int pageSize, string? templateKey, string? toEmail, string? status, CancellationToken ct = default);
}

