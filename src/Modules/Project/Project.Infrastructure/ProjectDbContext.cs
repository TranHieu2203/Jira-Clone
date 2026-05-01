using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Project.Domain;

namespace Project.Infrastructure;

public sealed class ProjectDbContext : BaseDbContext
{
    public const string Schema = "project";

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Domain.Project> Projects => Set<Domain.Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();

    public ProjectDbContext(
        DbContextOptions<ProjectDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<Workspace>(e =>
        {
            e.ToTable("workspaces");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => x.Slug).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.OwnerId);
            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Navigation(x => x.Members).Metadata.SetField("_members");
            e.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<WorkspaceMember>(e =>
        {
            e.ToTable("workspace_members");
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        b.Entity<Domain.Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Key).HasMaxLength(10).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => new { x.WorkspaceId, x.Key }).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.LeadId);
            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Members).Metadata.SetField("_members");
            e.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasMany(x => x.IssueTypes)
                .WithOne()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.IssueTypes).Metadata.SetField("_issueTypes");
            e.Navigation(x => x.IssueTypes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<ProjectMember>(e =>
        {
            e.ToTable("project_members");
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        b.Entity<IssueType>(e =>
        {
            e.ToTable("issue_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(16);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
        });

        base.OnModelCreating(b);
    }
}
