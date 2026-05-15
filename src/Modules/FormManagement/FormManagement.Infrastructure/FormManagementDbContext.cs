using BB.Common;
using BB.Persistence;
using BB.Web;
using FormManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormManagement.Infrastructure;

public sealed class FormManagementDbContext : BaseDbContext
{
    public const string Schema = "form_mgmt";

    public DbSet<MetadataDef> Metadata => Set<MetadataDef>();
    public DbSet<DocumentTemplate> Templates => Set<DocumentTemplate>();
    public DbSet<Submission> Submissions => Set<Submission>();

    public FormManagementDbContext(
        DbContextOptions<FormManagementDbContext> options,
        ICorrelationContext? correlation = null,
        IDomainEventDispatcher? eventDispatcher = null,
        IClock? clock = null)
        : base(options, correlation, eventDispatcher, clock) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres) b.HasDefaultSchema(Schema);

        b.Entity<MetadataDef>(e =>
        {
            e.ToTable("metadata");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(50).IsRequired();
            e.Property(x => x.Label).HasMaxLength(255).IsRequired();
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.FieldGroup).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(2000);
            // text columns: explicit type per provider để Oracle.EntityFrameworkCore không null-ref khi diff
            // (Postgres "text", Oracle "CLOB"). Pattern theo Issue.Infrastructure.
            e.Property(x => x.ValidationJson).HasColumnType(isPostgres ? "text" : "CLOB");
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => x.Value).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.FieldGroup);
            e.Ignore(x => x.DomainEvents);
        });

        b.Entity<DocumentTemplate>(e =>
        {
            e.ToTable("templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<int>();
            // SfdtContent + UsedFieldsJson: text/CLOB; DocxBytes: bytea/BLOB. Explicit per provider.
            e.Property(x => x.SfdtContent).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.UsedFieldsJson).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.DocxBytes).HasColumnType(isPostgres ? "bytea" : "BLOB");
            // S3 storage key (nullable cho transition period — template cũ vẫn dùng DocxBytes).
            // Max length 512 đủ cho path dạng `templates/{guid}/v{n}.docx` + prefix tương lai.
            e.Property(x => x.StorageKey).HasMaxLength(512);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => x.Code).IsUnique()
                .HasFilter(isPostgres ? "is_deleted = false" : null);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Category);
            e.Ignore(x => x.DomainEvents);
        });

        b.Entity<Submission>(e =>
        {
            e.ToTable("submissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExportFormat).HasConversion<int>();
            e.Property(x => x.DataJson).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.OutputPath).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            e.HasIndex(x => x.TemplateId);
            e.Ignore(x => x.DomainEvents);
        });

        base.OnModelCreating(b);
    }
}
