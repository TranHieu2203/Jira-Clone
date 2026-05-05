using AuditLog.Domain;
using BB.Common;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;

namespace AuditLog.Infrastructure;

public sealed class AuditLogDbContext : BaseDbContext
{
    public const string Schema = "audit";

    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    public AuditLogDbContext(
        DbContextOptions<AuditLogDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        string providerName = Database.ProviderName ?? string.Empty;
        bool isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<AuditEntry>(e =>
        {
            e.ToTable("audit_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(AuditEntry.ActionMaxLength).IsRequired();
            e.Property(x => x.Scope).HasMaxLength(AuditEntry.ScopeMaxLength).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType(isPostgres ? "text" : "CLOB");
            e.Property(x => x.TraceId).HasMaxLength(64);
            // AuditEntry kế thừa BaseEntity (không AuditableEntity) — entry là immutable,
            // không cần CreatedBy/UpdatedBy. ActorUserId + OccurredAt + TraceId đã đủ.

            // Filter chính: theo time + scope/action.
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => new { x.Scope, x.OccurredAt });
            e.HasIndex(x => new { x.Action, x.OccurredAt });
            e.HasIndex(x => x.ScopeId);
            e.HasIndex(x => x.ActorUserId);
        });

        base.OnModelCreating(b);
    }
}
