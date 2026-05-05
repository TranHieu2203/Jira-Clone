using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Notification.Application.Infrastructure;

namespace Notification.Infrastructure.Email;

public sealed class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.resend.com";
}

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly ResendOptions _opts;

    public ResendEmailSender(HttpClient http, IOptions<ResendOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public async Task<EmailSendResult> SendAsync(
        string fromEmail,
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            throw new InvalidOperationException("Resend API key is missing.");

        var payload = new Dictionary<string, object?>
        {
            ["from"] = fromEmail,
            ["to"] = new[] { toEmail },
            ["subject"] = subject,
            ["html"] = htmlBody
        };
        if (!string.IsNullOrWhiteSpace(textBody))
            payload["text"] = textBody;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opts.BaseUrl.TrimEnd('/')}/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage res = await _http.SendAsync(req, ct);
        string body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Resend send failed ({(int)res.StatusCode}): {body}");

        using JsonDocument doc = JsonDocument.Parse(body);
        string? id = doc.RootElement.TryGetProperty("id", out JsonElement e) ? e.GetString() : null;
        return new EmailSendResult("resend", id);
    }
}

