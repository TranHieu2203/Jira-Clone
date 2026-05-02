using Attachment.Domain;
using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;

namespace Attachment.Infrastructure;

public sealed class AttachmentDbContext : BaseDbContext
{
    public const string Schema = "attachment";

    public DbSet<IssueAttachment> Attachments => Set<IssueAttachment>();

    public AttachmentDbContext(
        DbContextOptions<AttachmentDbContext> options,
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

        b.Entity<IssueAttachment>(e =>
        {
            e.ToTable("issue_attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            e.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.IssueId);
            e.HasIndex(x => new { x.IssueId, x.CreatedAt });
            e.HasIndex(x => x.StorageKey).IsUnique();
        });

        base.OnModelCreating(b);
    }
}
