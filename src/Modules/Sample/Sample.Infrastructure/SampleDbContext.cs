using BB.Persistence;
using BB.Web;
using Microsoft.EntityFrameworkCore;
using Sample.Domain;

namespace Sample.Infrastructure;

public sealed class SampleDbContext : BaseDbContext
{
    public const string Schema = "sample";

    public DbSet<Product> Products => Set<Product>();

    public SampleDbContext(DbContextOptions<SampleDbContext> options, ICorrelationContext? correlation = null)
        : base(options, correlation) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
        {
            b.HasDefaultSchema(Schema);
        }

        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Sku).HasMaxLength(64).IsRequired();
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.Price).HasPrecision(18, 4);
            e.Property(p => p.CreatedBy).HasMaxLength(64);
            e.Property(p => p.UpdatedBy).HasMaxLength(64);
            e.Property(p => p.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Name);
        });

        base.OnModelCreating(b);
    }
}
