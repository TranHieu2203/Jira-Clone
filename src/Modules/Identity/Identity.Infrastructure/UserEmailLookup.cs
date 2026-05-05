using BB.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public sealed class UserEmailLookup : IUserEmailLookup
{
    private readonly IdentityDbContext _db;

    public UserEmailLookup(IdentityDbContext db) => _db = db;

    public Task<string?> FindEmailByIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> FindEmailsByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        Guid[] ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, string>();

        var rows = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        Dictionary<Guid, string> dict = new();
        foreach (var r in rows)
        {
            if (!string.IsNullOrWhiteSpace(r.Email))
                dict[r.Id] = r.Email;
        }

        return dict;
    }
}

