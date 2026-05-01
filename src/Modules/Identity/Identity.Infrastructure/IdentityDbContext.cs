using BB.Persistence;
using BB.Web;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public sealed class IdentityDbContext : BaseDbContext
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICorrelationContext? correlation = null)
        : base(options, correlation) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerName = Database.ProviderName ?? string.Empty;
        var isPostgres = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (isPostgres)
        {
            b.HasDefaultSchema(Schema);
        }

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(64);
            e.Property(x => x.UpdatedBy).HasMaxLength(64);
            e.Property(x => x.CreatedTraceId).HasMaxLength(64);
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.RefreshTokens).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.Description).HasMaxLength(256);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(512).IsRequired();
            e.Property(x => x.ReplacedByToken).HasMaxLength(512);
            e.HasIndex(x => x.Token).IsUnique();
        });

        base.OnModelCreating(b);
    }
}
