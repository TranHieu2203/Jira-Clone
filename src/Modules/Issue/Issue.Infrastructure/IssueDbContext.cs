using BB.Common;
using BB.Persistence;
using BB.Web;
using Issue.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Issue.Infrastructure;

public sealed class IssueDbContext : BaseDbContext
{
    public const string Schema = "issue";

    public DbSet<Domain.Issue> Issues => Set<Domain.Issue>();
    public DbSet<IssueWatcher> Watchers => Set<IssueWatcher>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();

    public IssueDbContext(
        DbContextOptions<IssueDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        var stringListConverter = new ValueConverter<List<string>, string>(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null)!,
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            a => a.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            a => a.ToList());

        b.Entity<Domain.Issue>(e =>
        {
            e.ToTable("issues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(20).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(Domain.Issue.SummaryMaxLength).IsRequired();
            e.Property(x => x.Description).HasColumnType(isPostgres ? "text" : "CLOB");
            e.Property(x => x.Priority).HasConversion<int>();
            e.Property(x => x.StoryPoints).HasPrecision(12, 2);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            var labelsProp = e.Property(x => x.Labels).HasConversion(stringListConverter);
            labelsProp.Metadata.SetValueComparer(stringListComparer);
            labelsProp.HasColumnType(isPostgres ? "jsonb" : "CLOB");

            e.HasIndex(x => x.Key).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => new { x.ProjectId, x.Number });
            e.HasIndex(x => x.AssigneeId);
            e.HasIndex(x => x.ReporterId);
            e.HasIndex(x => x.CurrentStatusId);
            e.HasIndex(x => x.IssueTypeId);
            e.HasIndex(x => x.ParentIssueId);

            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Watchers)
                .WithOne()
                .HasForeignKey(w => w.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Watchers).Metadata.SetField("_watchers");
            e.Navigation(x => x.Watchers).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<IssueWatcher>(e =>
        {
            e.ToTable("issue_watchers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.IssueId, x.UserId }).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        // F2: saved filter — JQL search lưu lại theo user.
        b.Entity<SavedFilter>(e =>
        {
            e.ToTable("saved_filters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(SavedFilter.NameMaxLength).IsRequired();
            e.Property(x => x.Jql).HasMaxLength(SavedFilter.JqlMaxLength).IsRequired();
            e.Property(x => x.Description).HasMaxLength(SavedFilter.DescriptionMaxLength);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            // Lookup: filter của 1 user + filter shared cho mọi user.
            e.HasIndex(x => x.OwnerUserId);
            e.HasIndex(x => x.IsShared);

            e.Ignore(x => x.DomainEvents);
        });

        base.OnModelCreating(b);
    }
}
