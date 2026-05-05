namespace Notification.Application.Infrastructure;

public interface ITemplateRenderer
{
    string Render(string template, IReadOnlyDictionary<string, string> args);
}

