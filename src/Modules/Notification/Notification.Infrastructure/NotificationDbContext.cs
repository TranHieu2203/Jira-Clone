using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Notification.Domain;

namespace Notification.Infrastructure;

public sealed class NotificationDbContext : BaseDbContext
{
    public const string Schema = "notification";

    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        string providerName = Database.ProviderName ?? string.Empty;
        bool isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres)
            b.HasDefaultSchema(Schema);

        b.Entity<InAppNotification>(e =>
        {
            e.ToTable("in_app_notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType(isPostgres ? "jsonb" : "CLOB").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => new { x.RecipientUserId, x.IsRead });
            e.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
        });

        b.Entity<EmailTemplate>(e =>
        {
            e.ToTable("email_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.SubjectTemplate).HasMaxLength(512).IsRequired();
            e.Property(x => x.HtmlBodyTemplate).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.TextBodyTemplate).HasColumnType(isPostgres ? "text" : "CLOB");
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.Key).IsUnique();
        });

        b.Entity<EmailLog>(e =>
        {
            e.ToTable("email_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.TemplateKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.ToEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.SubjectRendered).HasMaxLength(512).IsRequired();
            e.Property(x => x.BodyPreview).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ArgsJson).HasColumnType(isPostgres ? "jsonb" : "CLOB");
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.Provider).HasMaxLength(64);
            e.Property(x => x.ProviderMessageId).HasMaxLength(128);
            e.Property(x => x.Error).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => new { x.TemplateKey, x.CreatedAt });
            e.HasIndex(x => new { x.ToEmail, x.CreatedAt });
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        base.OnModelCreating(b);
    }
}
