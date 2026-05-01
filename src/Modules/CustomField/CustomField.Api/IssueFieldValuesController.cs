using BB.Web;
using CustomField.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomField.Api;

[ApiController]
[Route("api/v1/issue-field-values")]
[Authorize]
public sealed class IssueFieldValuesController : BaseController
{
    private readonly IIssueFieldValueService _service;
    public IssueFieldValuesController(IIssueFieldValueService service) => _service = service;

    [HttpGet("{issueId:guid}")]
    public async Task<IActionResult> ListByIssue(Guid issueId, CancellationToken ct) =>
        ToResponse(await _service.ListByIssueAsync(issueId, ct));

    [HttpPut]
    public async Task<IActionResult> SetValues([FromBody] SetIssueFieldValuesRequest request, CancellationToken ct) =>
        ToResponse(await _service.SetValuesAsync(request, ct));

    [HttpDelete("{issueId:guid}")]
    public async Task<IActionResult> ClearForIssue(Guid issueId, CancellationToken ct) =>
        ToResponse(await _service.ClearForIssueAsync(issueId, ct));
}
