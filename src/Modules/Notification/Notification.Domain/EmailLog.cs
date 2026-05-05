using BB.Common;

namespace Notification.Domain;

public sealed class EmailLog : AuditableEntity
{
    public string TemplateKey { get; private set; } = string.Empty;
    public string ToEmail { get; private set; } = string.Empty;
    public string SubjectRendered { get; private set; } = string.Empty;
    public string BodyPreview { get; private set; } = string.Empty;
    public string? ArgsJson { get; private set; }

    public EmailLogStatus Status { get; private set; } = EmailLogStatus.Pending;
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    private EmailLog() { }

    public EmailLog(
        string templateKey,
        string toEmail,
        string subjectRendered,
        string bodyPreview,
        string? argsJson)
    {
        TemplateKey = templateKey;
        ToEmail = toEmail;
        SubjectRendered = subjectRendered;
        BodyPreview = bodyPreview;
        ArgsJson = argsJson;
    }

    public void MarkSent(string provider, string? providerMessageId, DateTimeOffset sentAt)
    {
        Status = EmailLogStatus.Sent;
        Provider = provider;
        ProviderMessageId = providerMessageId;
        SentAt = sentAt;
        Error = null;
    }

    public void MarkFailed(string provider, string error)
    {
        Status = EmailLogStatus.Failed;
        Provider = provider;
        Error = error;
    }

    public void MarkSkipped(string provider, string reason)
    {
        Status = EmailLogStatus.Skipped;
        Provider = provider;
        Error = reason;
    }
}

