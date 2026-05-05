namespace Notification.Application.Infrastructure;

public sealed record EmailSendResult(string Provider, string? ProviderMessageId);

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string fromEmail,
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken ct = default);
}

