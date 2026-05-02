using BB.Web;
using Identity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : BaseController
{
    private readonly IUserSearchService _service;

    public UsersController(IUserSearchService service) => _service = service;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int take = 20, CancellationToken ct = default) =>
        ToResponse(await _service.SearchAsync(q, take, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default) =>
        ToResponse(await _service.GetByIdAsync(id, ct));
}
