using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application;

namespace Project.Api;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : BaseController
{
    private readonly IProjectService _service;
    public ProjectsController(IProjectService service) => _service = service;

    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(CancellationToken ct) =>
        ToResponse(await _service.ListMineAsync(ct));

    [HttpGet("by-workspace/{workspaceId:guid}")]
    public async Task<IActionResult> ListByWorkspace(Guid workspaceId, CancellationToken ct) =>
        ToResponse(await _service.ListByWorkspaceAsync(workspaceId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-key/{workspaceId:guid}/{key}")]
    public async Task<IActionResult> GetByKey(Guid workspaceId, string key, CancellationToken ct) =>
        ToResponse(await _service.GetByKeyAsync(workspaceId, key, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) =>
        ToResponse(await _service.ArchiveAsync(id, ct));

    [HttpPost("{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) =>
        ToResponse(await _service.UnarchiveAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    // ========== Members ==========
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddProjectMemberRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddMemberAsync(id, request, ct));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct) =>
        ToResponse(await _service.RemoveMemberAsync(id, userId, ct));

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeProjectMemberRoleRequest request, CancellationToken ct) =>
        ToResponse(await _service.ChangeMemberRoleAsync(id, userId, request, ct));

    // ========== Issue Types ==========
    [HttpPost("{id:guid}/issue-types")]
    public async Task<IActionResult> AddIssueType(Guid id, [FromBody] AddIssueTypeRequest request, CancellationToken ct) =>
        ToResponse(await _service.AddIssueTypeAsync(id, request, ct));

    [HttpPut("{id:guid}/issue-types/{issueTypeId:guid}")]
    public async Task<IActionResult> UpdateIssueType(Guid id, Guid issueTypeId, [FromBody] UpdateIssueTypeRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateIssueTypeAsync(id, issueTypeId, request, ct));

    [HttpDelete("{id:guid}/issue-types/{issueTypeId:guid}")]
    public async Task<IActionResult> RemoveIssueType(Guid id, Guid issueTypeId, CancellationToken ct) =>
        ToResponse(await _service.RemoveIssueTypeAsync(id, issueTypeId, ct));
}
