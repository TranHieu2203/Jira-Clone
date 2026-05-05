using BB.Common;
using BB.Persistence;
using BB.Web;
using IssueLink.Domain;
using Microsoft.EntityFrameworkCore;

namespace IssueLink.Infrastructure;

public sealed class IssueLinkDbContext : BaseDbContext
{
    public const string Schema = "issue_link";

    public DbSet<Domain.IssueLink> Links => Set<Domain.IssueLink>();

    public IssueLinkDbContext(
        DbContextOptions<IssueLinkDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        string providerName = Database.ProviderName ?? string.Empty;
        bool isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<Domain.IssueLink>(e =>
        {
            e.ToTable("issue_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.LinkType).HasConversion<int>();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            // Idempotency: cấm trùng cặp (source, target, type).
            e.HasIndex(x => new { x.SourceIssueId, x.TargetIssueId, x.LinkType }).IsUnique();

            // Lookup nhanh theo issue ở 2 đầu (cho ListByIssue tab issue detail).
            e.HasIndex(x => x.SourceIssueId);
            e.HasIndex(x => x.TargetIssueId);

            e.Ignore(x => x.DomainEvents);
        });

        base.OnModelCreating(b);
    }
}
