using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Workflow.Domain;

namespace Workflow.Infrastructure;

public sealed class WorkflowDbContext : BaseDbContext
{
    public const string Schema = "workflow";

    public DbSet<Domain.Workflow> Workflows => Set<Domain.Workflow>();
    public DbSet<WorkflowStatus> Statuses => Set<WorkflowStatus>();
    public DbSet<WorkflowTransition> Transitions => Set<WorkflowTransition>();
    public DbSet<TransitionRule> TransitionRules => Set<TransitionRule>();
    public DbSet<TransitionValidator> TransitionValidators => Set<TransitionValidator>();
    public DbSet<TransitionPostFunction> TransitionPostFunctions => Set<TransitionPostFunction>();
    public DbSet<WorkflowScheme> Schemes => Set<WorkflowScheme>();
    public DbSet<WorkflowSchemeItem> SchemeItems => Set<WorkflowSchemeItem>();
    public DbSet<IssueStatusHistory> IssueStatusHistories => Set<IssueStatusHistory>();

    public WorkflowDbContext(
        DbContextOptions<WorkflowDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<Domain.Workflow>(e =>
        {
            e.ToTable("workflows");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Key).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.IsTemplate);

            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Statuses)
                .WithOne()
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Transitions)
                .WithOne()
                .HasForeignKey(t => t.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Navigation(x => x.Statuses).Metadata.SetField("_statuses");
            e.Navigation(x => x.Statuses).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Transitions).Metadata.SetField("_transitions");
            e.Navigation(x => x.Transitions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<WorkflowStatus>(e =>
        {
            e.ToTable("workflow_statuses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Key).HasMaxLength(50).IsRequired();
            e.Property(x => x.Color).HasMaxLength(16);
            e.Property(x => x.Category).HasConversion<int>();
            e.HasIndex(x => new { x.WorkflowId, x.Key }).IsUnique();
        });

        b.Entity<WorkflowTransition>(e =>
        {
            e.ToTable("workflow_transitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.WorkflowId, x.FromStatusId, x.ToStatusId });

            e.HasMany(x => x.Rules).WithOne().HasForeignKey(r => r.TransitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Validators).WithOne().HasForeignKey(r => r.TransitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PostFunctions).WithOne().HasForeignKey(r => r.TransitionId).OnDelete(DeleteBehavior.Cascade);

            e.Navigation(x => x.Rules).Metadata.SetField("_rules");
            e.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Validators).Metadata.SetField("_validators");
            e.Navigation(x => x.Validators).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.PostFunctions).Metadata.SetField("_postFunctions");
            e.Navigation(x => x.PostFunctions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        ConfigureTransitionStep<TransitionRule>(b, "transition_rules");
        ConfigureTransitionStep<TransitionValidator>(b, "transition_validators");
        ConfigureTransitionStep<TransitionPostFunction>(b, "transition_post_functions");

        b.Entity<WorkflowScheme>(e =>
        {
            e.ToTable("workflow_schemes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.ProjectId).IsUnique();
            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.SchemeId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Items).Metadata.SetField("_items");
            e.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<WorkflowSchemeItem>(e =>
        {
            e.ToTable("workflow_scheme_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SchemeId, x.IssueTypeId }).IsUnique();
        });

        b.Entity<IssueStatusHistory>(e =>
        {
            e.ToTable("issue_status_histories");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChangedBy).HasMaxLength(64).IsRequired();
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.IssueId);
            e.HasIndex(x => new { x.IssueId, x.ChangedAt });
        });

        base.OnModelCreating(b);
    }

    private static void ConfigureTransitionStep<T>(ModelBuilder b, string tableName) where T : TransitionStep
    {
        b.Entity<T>(e =>
        {
            e.ToTable(tableName);
            e.HasKey(x => x.Id);
            e.Property(x => x.TypeKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.ConfigJson).HasMaxLength(4000).IsRequired();
            e.HasIndex(x => new { x.TransitionId, x.Order });
        });
    }
}
