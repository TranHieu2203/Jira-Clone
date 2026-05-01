using BB.Security;
using BB.Web;
using Identity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : BaseController
{
    private readonly IAuthService _service;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        ToResponse(await _service.LoginAsync(request, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) =>
        ToResponse(await _service.RefreshAsync(request, ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct) =>
        ToResponse(await _service.LogoutAsync(request.RefreshToken, ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }
        return ToResponse(await _service.MeAsync(_currentUser.UserId.Value, ct));
    }
}
