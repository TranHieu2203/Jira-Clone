using BB.Common;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BB.Persistence;

public abstract class BaseDbContext : DbContext
{
    private readonly ICorrelationContext? _correlation;

    protected BaseDbContext(DbContextOptions options, ICorrelationContext? correlation = null) : base(options)
    {
        _correlation = correlation;
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

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(ct);
    }

    private void ApplyAuditing()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (EntityEntry e in ChangeTracker.Entries())
        {
            if (e.Entity is IAuditable a)
            {
                if (e.State == EntityState.Added)
                {
                    a.CreatedAt = now;
                }
                else if (e.State == EntityState.Modified)
                {
                    a.UpdatedAt = now;
                }
            }
            if (e.Entity is IEntityWithTrace t && e.State == EntityState.Added && _correlation is not null)
            {
                t.CreatedTraceId = _correlation.TraceId;
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
