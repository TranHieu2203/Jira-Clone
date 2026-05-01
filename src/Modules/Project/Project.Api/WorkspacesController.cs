using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application;

namespace Project.Api;

[ApiController]
[Route("api/v1/workspaces")]
[Authorize]
public sealed class WorkspacesController : BaseController
{
    private readonly IWorkspaceService _service;
    public WorkspacesController(IWorkspaceService service) => _service = service;

    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(CancellationToken ct) =>
        ToResponse(await _service.ListMineAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) =>
        ToResponse(await _service.GetBySlugAsync(slug, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddWorkspaceMemberRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddMemberAsync(id, request, ct));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct) =>
        ToResponse(await _service.RemoveMemberAsync(id, userId, ct));

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeWorkspaceMemberRoleRequest request, CancellationToken ct) =>
        ToResponse(await _service.ChangeMemberRoleAsync(id, userId, request, ct));
}
