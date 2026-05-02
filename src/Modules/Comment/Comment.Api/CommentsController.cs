using BB.Web;
using Comment.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comment.Api;

[ApiController]
[Route("api/v1/comments")]
[Authorize]
public sealed class CommentsController : BaseController
{
    private readonly ICommentService _service;
    public CommentsController(ICommentService service) => _service = service;

    [HttpGet("by-issue/{issueId:guid}")]
    public async Task<IActionResult> ListByIssue(Guid issueId,
        [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListByIssueAsync(issueId, pageIndex, pageSize, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
