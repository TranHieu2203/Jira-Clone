using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Sprint.Domain;

namespace Sprint.Infrastructure;

public sealed class SprintDbContext : BaseDbContext
{
    public const string Schema = "sprint";

    public DbSet<Sprint.Domain.Sprint> Sprints => Set<Sprint.Domain.Sprint>();
    public DbSet<SprintIssue> SprintIssues => Set<SprintIssue>();
    public DbSet<SprintCommitLine> SprintCommitLines => Set<SprintCommitLine>();

    public SprintDbContext(
        DbContextOptions<SprintDbContext> options,
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

        b.Entity<Sprint.Domain.Sprint>(e =>
        {
            e.ToTable("sprints");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Goal).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => new { x.ProjectId, x.Status });

            e.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(si => si.SprintId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Items).Metadata.SetField("_items");
            e.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<SprintIssue>(e =>
        {
            e.ToTable("sprint_issues");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SprintId, x.IssueId }).IsUnique();
            e.HasIndex(x => x.IssueId);
        });

        b.Entity<SprintCommitLine>(e =>
        {
            e.ToTable("sprint_commit_lines");
            e.HasKey(x => x.Id);
            e.Property(x => x.BurndownPoints).HasPrecision(12, 2);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => new { x.SprintId, x.IssueId }).IsUnique();
        });

        base.OnModelCreating(b);
    }
}
