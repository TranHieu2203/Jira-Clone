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

        base.OnModelCreating(b);
    }
}
