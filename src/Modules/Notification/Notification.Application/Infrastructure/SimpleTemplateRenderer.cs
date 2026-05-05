using System.Text.RegularExpressions;

namespace Notification.Application.Infrastructure;

/// <summary>
/// Renderer đơn giản cho token dạng {{key}}. Không hỗ trợ logic / loop (MVP).
/// </summary>
public sealed class SimpleTemplateRenderer : ITemplateRenderer
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Render(string template, IReadOnlyDictionary<string, string> args)
    {
        if (string.IsNullOrEmpty(template) || args.Count == 0)
            return template;

        return TokenRegex.Replace(template, (m) =>
        {
            string key = m.Groups["key"].Value;
            return args.TryGetValue(key, out string? value) ? value : m.Value;
        });
    }
}

