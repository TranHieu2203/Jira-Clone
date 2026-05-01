using System.Linq.Expressions;
using BB.Common;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BB.Persistence;

public abstract class BaseDbContext : DbContext
{
    private readonly ICorrelationContext? _correlation;
    private readonly IDomainEventDispatcher? _eventDispatcher;
    private readonly IClock? _clock;

    protected BaseDbContext(
        DbContextOptions options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null) : base(options)
    {
        _correlation = correlation;
        _eventDispatcher = eventDispatcher;
        _clock = clock;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
    {
        cfg.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetConverter>();
        var providerName = Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            cfg.Properties<bool>().HaveConversion<BoolToNumberConverter>();
        }
        base.ConfigureConventions(cfg);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto query filter cho ISoftDeletable: các bản ghi IsDeleted=true bị ẩn ở mọi query.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(notDeleted, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAuditing();
        ApplySoftDelete();

        var aggregateRoots = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregateRoots.SelectMany(a => a.DomainEvents).ToList();

        var result = await base.SaveChangesAsync(ct);

        if (events.Count > 0 && _eventDispatcher is not null)
        {
            await _eventDispatcher.DispatchAsync(events, ct);
            foreach (var root in aggregateRoots) root.ClearDomainEvents();
        }

        return result;
    }

    private void ApplyAuditing()
    {
        var now = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
        foreach (EntityEntry e in ChangeTracker.Entries())
        {
            if (e.Entity is IAuditable a)
            {
                if (e.State == EntityState.Added) a.CreatedAt = now;
                else if (e.State == EntityState.Modified) a.UpdatedAt = now;
            }
            if (e.Entity is IEntityWithTrace t && e.State == EntityState.Added && _correlation is not null)
            {
                t.CreatedTraceId = _correlation.TraceId;
            }
        }
    }

    private void ApplySoftDelete()
    {
        var now = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
        foreach (var e in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (e.State == EntityState.Deleted)
            {
                e.State = EntityState.Modified;
                e.Entity.IsDeleted = true;
                e.Entity.DeletedAt = now;
            }
        }
    }
}

public sealed class DateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTime>
{
    public DateTimeOffsetConverter() : base(
        v => v.UtcDateTime,
        v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)))
    { }
}

public sealed class BoolToNumberConverter : ValueConverter<bool, int>
{
    public BoolToNumberConverter() : base(v => v ? 1 : 0, v => v != 0) { }
}
