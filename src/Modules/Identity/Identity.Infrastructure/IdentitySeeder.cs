using Identity.Application;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IdentityDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        // Schema already ensured by host bootstrap (EnsureCreated/Migrate).
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", ct);
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin", Description = "System administrator" };
            db.Roles.Add(adminRole);
            db.Roles.Add(new Role { Name = "User", Description = "Default user" });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Users.AnyAsync(ct))
        {
            var admin = new User
            {
                UserName = "admin",
                Email = "admin@local",
                DisplayName = "Administrator",
                PasswordHash = hasher.Hash("Admin@123"),
                IsActive = true
            };
            admin.Roles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }
    }
}
