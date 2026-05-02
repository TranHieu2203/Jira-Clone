using Identity.Application;
using Identity.Domain;

namespace Identity.Infrastructure;

public sealed class UserNameLookup : IUserNameLookup
{
    private readonly IUserRepository _users;

    public UserNameLookup(IUserRepository users) => _users = users;

    public async Task<Guid?> FindActiveUserIdByUserNameAsync(string userName, CancellationToken ct = default)
    {
        User? u = await _users.FindByUserNameAsync(userName, ct);
        return u?.Id;
    }
}
