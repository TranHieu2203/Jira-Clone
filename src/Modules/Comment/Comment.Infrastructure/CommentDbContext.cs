using BB.Common;
using BB.Persistence;
using BB.Web;
using Comment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Comment.Infrastructure;

public sealed class CommentDbContext : BaseDbContext
{
    public const string Schema = "comment";

    public DbSet<Domain.Comment> Comments => Set<Domain.Comment>();

    public CommentDbContext(
        DbContextOptions<CommentDbContext> options,
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

        b.Entity<Domain.Comment>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasColumnType(isPostgres ? "text" : "CLOB").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.DeletedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);

            // Mentions là IReadOnlyList<string> (public) backed bởi field _mentions (List<string>).
            // Map qua field để EF không yêu cầu setter.
            var mentionsProp = e.Property<List<string>>("_mentions")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasField("_mentions")
                .HasColumnName("mentions")
                .HasConversion(stringListConverter);
            mentionsProp.Metadata.SetValueComparer(stringListComparer);
            mentionsProp.HasColumnType(isPostgres ? "jsonb" : "CLOB");
            e.Ignore(x => x.Mentions);

            e.HasIndex(x => x.IssueId);
            e.HasIndex(x => new { x.IssueId, x.CreatedAt });
            e.HasIndex(x => x.AuthorId);
            e.Ignore(x => x.DomainEvents);
        });

        base.OnModelCreating(b);
    }
}
