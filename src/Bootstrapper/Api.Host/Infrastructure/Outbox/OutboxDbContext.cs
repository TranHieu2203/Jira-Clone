using BB.Common;
using BB.EventBus.Outbox;
using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;

namespace Api.Host.Infrastructure.Outbox;

/// <summary>Schema chung <c>outbox</c> — tái sử dụng DB với các module khác.</summary>
public sealed class OutboxDbContext : BaseDbContext
{
    public const string SchemaName = "outbox";

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public OutboxDbContext(
        DbContextOptions<OutboxDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? dispatcher = null,
        IClock? clock = null)
        : base(options, correlation, dispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        bool isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres)
            modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(1024).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.Error).HasMaxLength(4000);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.ProcessedAt);
            e.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        });

        base.OnModelCreating(modelBuilder);
    }
}
