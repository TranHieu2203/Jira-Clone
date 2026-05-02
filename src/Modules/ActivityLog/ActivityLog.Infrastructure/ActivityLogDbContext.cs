using ActivityLog.Domain;
using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;

namespace ActivityLog.Infrastructure;

public sealed class ActivityLogDbContext : BaseDbContext
{
    public const string Schema = "activity_log";

    public DbSet<ActivityEntry> ActivityEntries => Set<ActivityEntry>();

    public ActivityLogDbContext(
        DbContextOptions<ActivityLogDbContext> options,
        ICorrelationContext? correlation = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher: null, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<ActivityEntry>(e =>
        {
            e.ToTable("activity_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.IssueId).IsRequired();
            e.Property(x => x.OccurredAt).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(192).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType(isPostgres ? "jsonb" : "CLOB");
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.IssueId);
            e.HasIndex(x => new { x.IssueId, x.OccurredAt });
        });

        base.OnModelCreating(b);
    }
}
