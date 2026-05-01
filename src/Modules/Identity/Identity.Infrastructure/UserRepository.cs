using BB.Persistence;
using Identity.Application;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    private readonly IdentityDbContext _ctx;

    public UserRepository(IdentityDbContext ctx) : base(ctx) => _ctx = ctx;

    public Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default) =>
        _ctx.Users.AsNoTracking()
            .Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public Task<User?> FindByRefreshTokenAsync(string token, CancellationToken ct = default) =>
        _ctx.Users.AsNoTracking()
            .Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), ct);

    public Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default) =>
        _ctx.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        _ctx.RefreshTokens.AddAsync(token, ct).AsTask();
}
