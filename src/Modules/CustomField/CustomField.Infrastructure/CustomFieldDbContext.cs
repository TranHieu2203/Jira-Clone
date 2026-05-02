using BB.Common;
using BB.Persistence;
using BB.Persistence.Json;
using BB.Web;
using CustomField.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CustomField.Infrastructure;

public sealed class CustomFieldDbContext : BaseDbContext
{
    public const string Schema = "custom_field";

    public DbSet<Domain.CustomField> Fields => Set<Domain.CustomField>();
    public DbSet<CustomFieldOption> Options => Set<CustomFieldOption>();
    public DbSet<CustomFieldContext> Contexts => Set<CustomFieldContext>();
    public DbSet<IssueFieldValue> IssueFieldValues => Set<IssueFieldValue>();

    public CustomFieldDbContext(
        DbContextOptions<CustomFieldDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        var provider = isPostgres ? DbProvider.Postgres : DbProvider.Oracle;
        if (isPostgres) b.HasDefaultSchema(Schema);

        // Converter cho List<Guid> qua JSON (cho ProjectIds / IssueTypeIds trong Context).
        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null)!,
            v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>());

        b.Entity<Domain.CustomField>(e =>
        {
            e.ToTable("custom_fields");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.ConfigJson).HasJsonColumn(provider).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => x.Key).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.IsSystem);
            e.Ignore(x => x.DomainEvents);

            e.HasMany(x => x.Options).WithOne().HasForeignKey(o => o.CustomFieldId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Options).Metadata.SetField("_options");
            e.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasMany(x => x.Contexts).WithOne().HasForeignKey(c => c.CustomFieldId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Contexts).Metadata.SetField("_contexts");
            e.Navigation(x => x.Contexts).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<CustomFieldOption>(e =>
        {
            e.ToTable("custom_field_options");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(200).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.CustomFieldId, x.ParentOptionId, x.Value }).IsUnique();
        });

        var guidListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            a => a.Aggregate(0, (h, g) => h ^ g.GetHashCode()),
            a => a.ToList());

        b.Entity<CustomFieldContext>(e =>
        {
            e.ToTable("custom_field_contexts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            e.Property(x => x.DefaultValueJson).HasColumnType(isPostgres ? "jsonb" : "CLOB");

            var projectIdsProp = e.Property(x => x.ProjectIds).HasConversion(guidListConverter);
            projectIdsProp.Metadata.SetValueComparer(guidListComparer);
            projectIdsProp.HasColumnType(isPostgres ? "jsonb" : "CLOB");

            var issueTypeIdsProp = e.Property(x => x.IssueTypeIds).HasConversion(guidListConverter);
            issueTypeIdsProp.Metadata.SetValueComparer(guidListComparer);
            issueTypeIdsProp.HasColumnType(isPostgres ? "jsonb" : "CLOB");

            e.HasIndex(x => x.CustomFieldId);
        });

        b.Entity<IssueFieldValue>(e =>
        {
            e.ToTable("issue_field_values");
            e.HasKey(x => x.Id);
            e.Property(x => x.ValueJson).HasJsonColumn(provider).IsRequired();
            e.Property(x => x.IndexedString).HasMaxLength(500);
            e.Property(x => x.IndexedNumber).HasPrecision(28, 8);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => new { x.IssueId, x.CustomFieldId }).IsUnique();
            e.HasIndex(x => new { x.CustomFieldId, x.IndexedString });
            e.HasIndex(x => new { x.CustomFieldId, x.IndexedNumber });
            e.HasIndex(x => new { x.CustomFieldId, x.IndexedDate });
        });

        base.OnModelCreating(b);
    }
}
