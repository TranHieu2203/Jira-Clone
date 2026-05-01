using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BB.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);
    public IReadOnlyList<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
