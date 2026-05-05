using System.Text.Json;
using BB.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Infrastructure;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application;

public sealed class EmailOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "resend";
    public string FromEmail { get; set; } = string.Empty;
}

public sealed class EmailService : IEmailService
{
    private const int BodyPreviewMaxLen = 2000;

    private readonly IEmailTemplateRepository _templates;
    private readonly IEmailLogRepository _logs;
    private readonly INotificationUnitOfWork _uow;
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailSender _sender;
    private readonly EmailOptions _opts;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailTemplateRepository templates,
        IEmailLogRepository logs,
        INotificationUnitOfWork uow,
        ITemplateRenderer renderer,
        IEmailSender sender,
        IOptions<EmailOptions> opts,
        ILogger<EmailService> logger)
    {
        _templates = templates;
        _logs = logs;
        _uow = uow;
        _renderer = renderer;
        _sender = sender;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<Result<EmailTemplateDto>> UpsertTemplateAsync(UpsertEmailTemplateRequest request, CancellationToken ct = default)
    {
        string key = (request.Key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
            return Result.Failure<EmailTemplateDto>(ErrorType.Validation, "validation.required");

        EmailTemplate? existing = await _templates.FindByKeyAsync(key, ct);
        if (existing is null)
        {
            var created = new EmailTemplate(
                key,
                request.Name,
                request.SubjectTemplate,
                request.HtmlBodyTemplate,
                request.TextBodyTemplate,
                request.IsEnabled);
            await _templates.AddAsync(created, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success(ToDto(created), messageKey: "email.template.saved");
        }

        existing.Update(
            request.Name,
            request.SubjectTemplate,
            request.HtmlBodyTemplate,
            request.TextBodyTemplate,
            request.IsEnabled);

        _templates.Update(existing);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(existing), messageKey: "email.template.saved");
    }

    public async Task<Result<PagedList<EmailTemplateDto>>> ListTemplatesAsync(int pageIndex, int pageSize, string? q, CancellationToken ct = default)
    {
        PagedList<EmailTemplate> page = await _templates.ListAsync(pageIndex, pageSize, q, ct);
        List<EmailTemplateDto> items = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<EmailTemplateDto>(items, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<EmailTemplateDto>> GetTemplateAsync(string key, CancellationToken ct = default)
    {
        EmailTemplate? t = await _templates.FindByKeyAsync(key, ct);
        return t is null
            ? Result.Failure<EmailTemplateDto>(ErrorType.NotFound, "email.template.not_found")
            : Result.Success(ToDto(t));
    }

    public async Task<Result<EmailLogDto>> SendAsync(SendEmailRequest request, CancellationToken ct = default)
    {
        string to = (request.ToEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(to))
            return Result.Failure<EmailLogDto>(ErrorType.Validation, "validation.required");

        string templateKey = (request.TemplateKey ?? string.Empty).Trim();
        EmailTemplate? tpl = await _templates.FindByKeyAsync(templateKey, ct);
        if (tpl is null)
            return Result.Failure<EmailLogDto>(ErrorType.NotFound, "email.template.not_found");

        if (!tpl.IsEnabled)
            return Result.Failure<EmailLogDto>(ErrorType.Conflict, "email.template.disabled");

        Dictionary<string, string> args = request.Args ?? new Dictionary<string, string>();
        string subject = _renderer.Render(tpl.SubjectTemplate, args);
        string html = _renderer.Render(tpl.HtmlBodyTemplate, args);
        string? text = tpl.TextBodyTemplate is null ? null : _renderer.Render(tpl.TextBodyTemplate, args);

        string preview = BuildPreview(text ?? html);
        string? argsJson = args.Count == 0 ? null : JsonSerializer.Serialize(args);

        var log = new EmailLog(templateKey, to, subject, preview, argsJson);
        await _logs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        if (!_opts.Enabled)
        {
            log.MarkSkipped(_opts.Provider, "email.sending.disabled");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return Result.Success(ToDto(log), messageKey: "email.send.skipped");
        }

        if (string.IsNullOrWhiteSpace(_opts.FromEmail))
        {
            log.MarkFailed(_opts.Provider, "email.from_missing");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return Result.Failure<EmailLogDto>(ErrorType.Validation, "email.from_missing");
        }

        try
        {
            EmailSendResult sent = await _sender.SendAsync(_opts.FromEmail, to, subject, html, text, ct);
            log.MarkSent(sent.Provider, sent.ProviderMessageId, DateTimeOffset.UtcNow);
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Email {LogId} sent via {Provider} to {To}", log.Id, sent.Provider, to);
            return Result.Success(ToDto(log), messageKey: "email.sent.success");
        }
        catch (Exception ex)
        {
            string err = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            log.MarkFailed(_opts.Provider, err);
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            _logger.LogError(ex, "Email {LogId} failed to {To}", log.Id, to);
            return Result.Failure<EmailLogDto>(ErrorType.Unexpected, "email.sent.failed");
        }
    }

    public async Task<Result<PagedList<EmailLogDto>>> ListLogsAsync(
        int pageIndex, int pageSize, string? templateKey, string? toEmail, string? status, CancellationToken ct = default)
    {
        EmailLogStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmailLogStatus>(status, ignoreCase: true, out var st))
            parsed = st;

        PagedList<EmailLog> page = await _logs.ListAsync(pageIndex, pageSize, templateKey, toEmail, parsed, ct);
        List<EmailLogDto> items = page.Items.Select(ToDto).ToList();
        return Result.Success(new PagedList<EmailLogDto>(items, page.TotalCount, page.PageIndex, page.PageSize));
    }

    public async Task<Result<EmailLogDto>> RetryAsync(Guid logId, CancellationToken ct = default)
    {
        EmailLog? original = await _logs.GetByIdAsync(logId, ct);
        if (original is null)
            return Result.Failure<EmailLogDto>(ErrorType.NotFound, "email.log.not_found");
        if (original.Status != EmailLogStatus.Failed)
            return Result.Failure<EmailLogDto>(ErrorType.Validation, "email.retry.only_failed");

        // Re-build args từ ArgsJson đã lưu trong log gốc.
        Dictionary<string, string> args = string.IsNullOrWhiteSpace(original.ArgsJson)
            ? new Dictionary<string, string>()
            : (JsonSerializer.Deserialize<Dictionary<string, string>>(original.ArgsJson) ?? new Dictionary<string, string>());

        // Reuse SendAsync — sẽ tạo log mới + mark Sent/Failed; log gốc giữ nguyên.
        return await SendAsync(new SendEmailRequest(original.TemplateKey, original.ToEmail, args), ct);
    }

    private static string BuildPreview(string content)
    {
        string raw = content.Replace("\r", string.Empty);
        return raw.Length <= BodyPreviewMaxLen ? raw : raw[..BodyPreviewMaxLen];
    }

    private static EmailTemplateDto ToDto(EmailTemplate t) =>
        new(t.Id, t.Key, t.Name, t.SubjectTemplate, t.HtmlBodyTemplate, t.TextBodyTemplate, t.IsEnabled, t.CreatedAt, t.UpdatedAt);

    private static EmailLogDto ToDto(EmailLog l) =>
        new(
            l.Id,
            l.TemplateKey,
            l.ToEmail,
            l.Status,
            l.Provider ?? "-",
            l.ProviderMessageId,
            l.SubjectRendered,
            l.BodyPreview,
            l.Error,
            l.CreatedAt,
            l.SentAt);
}

