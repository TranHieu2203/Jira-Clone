using Identity.Application;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure;

public static class IdentitySeeder
{
    /// <summary>User dev thứ hai: cùng quyền/mật khẩu mặc định như <c>admin</c>, dùng login rồi tag <c>@admin</c>.</summary>
    public const string DevSecondAdminUserName = "admin2";

    /// <summary>Mật khẩu seed — trùng <c>admin</c> (chỉ môi trường dev).</summary>
    public const string DevDefaultAdminPassword = "Admin@123";

    private const string DevSecondAdminEmail = "admin2@local";

    public static async Task SeedAsync(IdentityDbContext db, IPasswordHasher hasher, IConfiguration cfg, CancellationToken ct = default)
    {
        // Schema already ensured by host bootstrap (EnsureCreated/Migrate).
        Role? adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", ct);
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin", Description = "System administrator" };
            db.Roles.Add(adminRole);
            db.Roles.Add(new Role { Name = "User", Description = "Default user" });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Users.AnyAsync(ct))
        {
            User admin = new()
            {
                UserName = "admin",
                Email = "admin@local",
                DisplayName = "Administrator",
                PasswordHash = hasher.Hash(DevDefaultAdminPassword),
                IsActive = true
            };
            admin.Roles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }

        await EnsureDevSecondAdminAsync(db, hasher, adminRole, ct);

        string? notifyEmail = cfg["E2E:NotifyUserEmail"];
        if (string.IsNullOrWhiteSpace(notifyEmail))
            return;

        notifyEmail = notifyEmail.Trim();
        User adminUser = await db.Users.FirstAsync(u => u.UserName == "admin", ct);
        if (!string.Equals(adminUser.Email, notifyEmail, StringComparison.OrdinalIgnoreCase))
        {
            adminUser.Email = notifyEmail;
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Luôn đảm bảo có <c>admin2</c> (role Admin, pass giống <c>admin</c>).</summary>
    private static async Task EnsureDevSecondAdminAsync(
        IdentityDbContext db,
        IPasswordHasher hasher,
        Role adminRole,
        CancellationToken ct)
    {
        User? u2 = await db.Users.FirstOrDefaultAsync(x => x.UserName == DevSecondAdminUserName, ct);
        if (u2 is null)
        {
            User u = new()
            {
                UserName = DevSecondAdminUserName,
                Email = DevSecondAdminEmail,
                DisplayName = "Admin 2",
                PasswordHash = hasher.Hash(DevDefaultAdminPassword),
                IsActive = true
            };
            u.Roles.Add(new UserRole { UserId = u.Id, RoleId = adminRole.Id });
            db.Users.Add(u);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(u2.Email) ||
            !string.Equals(u2.Email, DevSecondAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            u2.Email = DevSecondAdminEmail;
            await db.SaveChangesAsync(ct);
        }

        bool hasAdminRole = await db.UserRoles
            .AnyAsync(ur => ur.UserId == u2.Id && ur.RoleId == adminRole.Id, ct);
        if (!hasAdminRole)
        {
            db.UserRoles.Add(new UserRole { UserId = u2.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync(ct);
        }
    }
}
