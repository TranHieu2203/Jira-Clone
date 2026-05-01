using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Localization;

public interface IAppLocalizer
{
    string Translate(string key, object? args = null, string? culture = null);
}

public sealed class JsonAppLocalizer : IAppLocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources;
    private readonly string _defaultCulture;

    public JsonAppLocalizer(string resourcesDir, string defaultCulture = "vi")
    {
        _defaultCulture = defaultCulture;
        _resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(resourcesDir))
        {
            foreach (var file in Directory.GetFiles(resourcesDir, "*.json"))
            {
                var lang = Path.GetFileNameWithoutExtension(file);
                var json = File.ReadAllText(file);
                var dict = Flatten(JsonDocument.Parse(json).RootElement);
                _resources[lang] = dict;
            }
        }
    }

    public string Translate(string key, object? args = null, string? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (!_resources.TryGetValue(culture, out var dict) &&
            !_resources.TryGetValue(_defaultCulture, out dict))
        {
            return key;
        }
        if (!dict.TryGetValue(key, out var template))
        {
            return key;
        }
        return Format(template, args);
    }

    private static string Format(string template, object? args)
    {
        if (args is null) return template;
        var result = template;
        foreach (var prop in args.GetType().GetProperties())
        {
            result = result.Replace("{{" + prop.Name + "}}", prop.GetValue(args)?.ToString() ?? string.Empty);
        }
        return result;
    }

    private static Dictionary<string, string> Flatten(JsonElement root, string prefix = "")
    {
        var result = new Dictionary<string, string>();
        foreach (var prop in root.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var kv in Flatten(prop.Value, key))
                {
                    result[kv.Key] = kv.Value;
                }
            }
            else
            {
                result[key] = prop.Value.ToString();
            }
        }
        return result;
    }
}

public static class LocalizationExtensions
{
    public static IServiceCollection AddBbLocalization(this IServiceCollection services, string resourcesDir, string defaultCulture = "vi")
    {
        services.AddSingleton<IAppLocalizer>(_ => new JsonAppLocalizer(resourcesDir, defaultCulture));
        return services;
    }
}
