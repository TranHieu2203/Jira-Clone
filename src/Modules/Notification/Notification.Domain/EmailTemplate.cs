using BB.Common;

namespace Notification.Domain;

public sealed class EmailTemplate : AuditableEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string HtmlBodyTemplate { get; private set; } = string.Empty;
    public string? TextBodyTemplate { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    private EmailTemplate() { }

    public EmailTemplate(
        string key,
        string name,
        string subjectTemplate,
        string htmlBodyTemplate,
        string? textBodyTemplate,
        bool isEnabled)
    {
        Key = key.Trim();
        Name = name.Trim();
        SubjectTemplate = subjectTemplate;
        HtmlBodyTemplate = htmlBodyTemplate;
        TextBodyTemplate = textBodyTemplate;
        IsEnabled = isEnabled;
    }

    public void Update(
        string name,
        string subjectTemplate,
        string htmlBodyTemplate,
        string? textBodyTemplate,
        bool isEnabled)
    {
        Name = name.Trim();
        SubjectTemplate = subjectTemplate;
        HtmlBodyTemplate = htmlBodyTemplate;
        TextBodyTemplate = textBodyTemplate;
        IsEnabled = isEnabled;
    }
}

