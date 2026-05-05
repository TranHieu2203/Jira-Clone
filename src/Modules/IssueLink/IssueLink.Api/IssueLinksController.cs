using BB.Web;
using IssueLink.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueLink.Api;

[ApiController]
[Route("api/v1/issue-links")]
[Authorize]
public sealed class IssueLinksController : BaseController
{
    private readonly IIssueLinkService _service;
    public IssueLinksController(IIssueLinkService service) => _service = service;

    /// <summary>Lấy outgoing + incoming link cho 1 issue (cho panel "Linked issues" trên issue detail).</summary>
    [HttpGet("by-issue/{issueId:guid}")]
    public async Task<IActionResult> ListByIssue(Guid issueId, CancellationToken ct = default) =>
        ToResponse(await _service.ListByIssueAsync(issueId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueLinkRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
