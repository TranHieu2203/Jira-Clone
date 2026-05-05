using System.Text.Json;
using BB.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Repositories;
using Notification.Domain;

namespace Notification.Application.Infrastructure;

/// <summary>
/// Email dispatcher cho integration-event handler: swallow lỗi để không làm fail outbox handler.
/// Luôn tạo log: Sent/Failed/Skipped.
/// </summary>
public sealed class EventEmailDispatcher : IEventEmailDispatcher
{
    private const int PreviewMaxLen = 2000;

    private readonly IUserEmailLookup _emails;
    private readonly IEmailTemplateRepository _templates;
    private readonly IEmailLogRepository _logs;
    private readonly INotificationUnitOfWork _uow;
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailSender _sender;
    private readonly EmailOptions _opts;
    private readonly ILogger<EventEmailDispatcher> _logger;

    public EventEmailDispatcher(
        IUserEmailLookup emails,
        IEmailTemplateRepository templates,
        IEmailLogRepository logs,
        INotificationUnitOfWork uow,
        ITemplateRenderer renderer,
        IEmailSender sender,
        IOptions<EmailOptions> opts,
        ILogger<EventEmailDispatcher> logger)
    {
        _emails = emails;
        _templates = templates;
        _logs = logs;
        _uow = uow;
        _renderer = renderer;
        _sender = sender;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task DispatchAsync(
        string templateKey,
        IReadOnlyCollection<Guid> recipientUserIds,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct = default)
    {
        if (recipientUserIds.Count == 0)
            return;

        IReadOnlyDictionary<Guid, string> emails = await _emails.FindEmailsByIdsAsync(recipientUserIds, ct);
        if (emails.Count == 0)
            return;

        EmailTemplate? tpl = await _templates.FindByKeyAsync(templateKey, ct);
        foreach (var kv in emails)
        {
            Guid userId = kv.Key;
            string toEmail = kv.Value;
            await SendOneAsync(tpl, templateKey, toEmail, args, ct);
        }
    }

    private async Task SendOneAsync(
        EmailTemplate? tpl,
        string templateKey,
        string toEmail,
        IReadOnlyDictionary<string, string> args,
        CancellationToken ct)
    {
        // log luôn có subject/bodyPreview kể cả skipped
        string subject = tpl is null ? $"[{templateKey}]" : _renderer.Render(tpl.SubjectTemplate, args);
        string html = tpl is null ? string.Empty : _renderer.Render(tpl.HtmlBodyTemplate, args);
        string? text = tpl?.TextBodyTemplate is null ? null : _renderer.Render(tpl.TextBodyTemplate, args);
        string preview = BuildPreview(string.IsNullOrWhiteSpace(text) ? html : text);
        string? argsJson = args.Count == 0 ? null : JsonSerializer.Serialize(args);

        var log = new EmailLog(templateKey, toEmail, subject, preview, argsJson);
        await _logs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        if (tpl is null)
        {
            log.MarkSkipped(_opts.Provider, "email.template.not_found");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        if (!tpl.IsEnabled)
        {
            log.MarkSkipped(_opts.Provider, "email.template.disabled");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        if (!_opts.Enabled)
        {
            log.MarkSkipped(_opts.Provider, "email.sending.disabled");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(_opts.FromEmail))
        {
            log.MarkFailed(_opts.Provider, "email.from_missing");
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        try
        {
            EmailSendResult sent = await _sender.SendAsync(_opts.FromEmail, toEmail, subject, html, text, ct);
            log.MarkSent(sent.Provider, sent.ProviderMessageId, DateTimeOffset.UtcNow);
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            string err = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            log.MarkFailed(_opts.Provider, err);
            _logs.Update(log);
            await _uow.SaveChangesAsync(ct);
            _logger.LogWarning(ex, "Event email failed ({TemplateKey}) to {To}", templateKey, toEmail);
        }
    }

    private static string BuildPreview(string content)
    {
        string raw = (content ?? string.Empty).Replace("\r", string.Empty);
        return raw.Length <= PreviewMaxLen ? raw : raw[..PreviewMaxLen];
    }
}

