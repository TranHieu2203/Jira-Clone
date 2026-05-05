using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Domain;

namespace Notification.Infrastructure.Seed;

/// <summary>
/// Seed mẫu email mặc định cho dispatcher (key khớp handler). Chỉ thêm khi chưa có — không ghi đè chỉnh sửa admin.
/// </summary>
public static class EmailTemplateSeeder
{
    public const string CommentAddedKey = "comment.added";
    public const string IssueStatusChangedKey = "issue.status_changed";
    public const string IssueAssigneeChangedKey = "issue.assignee_changed";

    public static async Task SeedDefaultsAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using IServiceScope scope = sp.CreateScope();
        NotificationDbContext ctx = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EmailTemplateSeeder");

        await EnsureOneAsync(ctx, logger, CommentAddedKey,
            "Comment added (default)",
            "[{{issueKey}}] New comment",
            """
            <p>There is a new comment on <strong>{{issueKey}}</strong>.</p>
            <p>Preview:</p>
            <div style="border-left:3px solid #ccc;padding-left:12px;margin:12px 0;">{{preview}}</div>
            """,
            "Issue {{issueKey}}\nPreview: {{preview}}",
            ct);

        await EnsureOneAsync(ctx, logger, IssueStatusChangedKey,
            "Issue status changed (default)",
            "[{{issueKey}}] Status updated",
            "<p>The status of <strong>{{issueKey}}</strong> has been updated.</p>",
            "The status of {{issueKey}} has been updated.",
            ct);

        await EnsureOneAsync(ctx, logger, IssueAssigneeChangedKey,
            "Issue assignee changed (default)",
            "[{{issueKey}}] You were assigned",
            "<p>You have been assigned to <strong>{{issueKey}}</strong>.</p>",
            "You have been assigned to {{issueKey}}.",
            ct);

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task EnsureOneAsync(
        NotificationDbContext ctx,
        ILogger logger,
        string key,
        string name,
        string subject,
        string html,
        string? text,
        CancellationToken ct)
    {
        bool exists = await ctx.EmailTemplates.AsNoTracking().AnyAsync(t => t.Key == key, ct);
        if (exists)
            return;

        ctx.EmailTemplates.Add(new EmailTemplate(key, name, subject, html, text, isEnabled: true));
        logger.LogInformation("Seeding email template {Key}", key);
    }
}
