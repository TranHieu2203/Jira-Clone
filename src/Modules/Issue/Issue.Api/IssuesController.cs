using BB.Web;
using Issue.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Issue.Api;

[ApiController]
[Route("api/v1/issues")]
[Authorize]
public sealed class IssuesController : BaseController
{
    private readonly IIssueService _service;
    public IssuesController(IIssueService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-key/{issueKey}")]
    public async Task<IActionResult> GetByKey(string issueKey, CancellationToken ct) =>
        ToResponse(await _service.GetByKeyAsync(issueKey, ct));

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchIssuesRequest request, CancellationToken ct) =>
        ToResponse(await _service.SearchAsync(request, ct));

    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> ListChildren(Guid id, CancellationToken ct) =>
        ToResponse(await _service.ListChildrenAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIssueRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) =>
        ToResponse(await _service.ArchiveAsync(id, ct));

    [HttpPost("{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) =>
        ToResponse(await _service.UnarchiveAsync(id, ct));

    [HttpPost("{id:guid}/transition")]
    public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionIssueRequest request, CancellationToken ct) =>
        ToResponse(await _service.TransitionAsync(id, request, ct));

    [HttpPost("{id:guid}/watchers/{userId:guid}")]
    public async Task<IActionResult> AddWatcher(Guid id, Guid userId, CancellationToken ct) =>
        ToResponse(await _service.AddWatcherAsync(id, userId, ct));

    [HttpDelete("{id:guid}/watchers/{userId:guid}")]
    public async Task<IActionResult> RemoveWatcher(Guid id, Guid userId, CancellationToken ct) =>
        ToResponse(await _service.RemoveWatcherAsync(id, userId, ct));
}
