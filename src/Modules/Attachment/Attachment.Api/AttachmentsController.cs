using Attachment.Application;
using BB.Common;
using BB.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attachment.Api;

[ApiController]
[Route("api/v1/issues/{issueId:guid}/attachments")]
[Authorize]
public sealed class AttachmentsController : BaseController
{
    private const long MaxUploadBytes = 12 * 1024 * 1024;

    private readonly IAttachmentService _service;

    public AttachmentsController(IAttachmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        Guid issueId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        ToResponse(await _service.ListByIssueAsync(issueId, pageIndex, pageSize, ct));

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(Guid issueId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return ToResponse(Result.Failure<AttachmentDto>(ErrorType.Validation, "validation.failed"));

        await using Stream stream = file.OpenReadStream();
        Result<AttachmentDto> result = await _service.UploadAsync(
            issueId,
            stream,
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            file.Length,
            ct);
        return Created(result);
    }

    [HttpGet("{attachmentId:guid}/file")]
    public async Task<IActionResult> Download(Guid issueId, Guid attachmentId, CancellationToken ct)
    {
        Result<AttachmentDownload> result = await _service.OpenDownloadAsync(issueId, attachmentId, ct);
        if (!result.IsSuccess)
            return ToResponse(result);

        AttachmentDownload d = result.Data!;
        return File(d.Stream, d.ContentType, d.FileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(Guid issueId, Guid attachmentId, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(issueId, attachmentId, ct));
}
