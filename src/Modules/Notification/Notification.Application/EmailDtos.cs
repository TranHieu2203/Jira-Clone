using Notification.Domain;

namespace Notification.Application;

public sealed record EmailTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string SubjectTemplate,
    string HtmlBodyTemplate,
    string? TextBodyTemplate,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertEmailTemplateRequest(
    string Key,
    string Name,
    string SubjectTemplate,
    string HtmlBodyTemplate,
    string? TextBodyTemplate,
    bool IsEnabled);

public sealed record EmailLogDto(
    Guid Id,
    string TemplateKey,
    string ToEmail,
    EmailLogStatus Status,
    string Provider,
    string? ProviderMessageId,
    string SubjectRendered,
    string BodyPreview,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

public sealed record SendEmailRequest(
    string TemplateKey,
    string ToEmail,
    Dictionary<string, string>? Args);

