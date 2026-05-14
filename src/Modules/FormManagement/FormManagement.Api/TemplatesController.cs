using BB.Web;
using FormManagement.Application;
using FormManagement.Application.Services;
using FormManagement.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FormManagement.Api;

[ApiController]
[Route("api/v1/form-management/templates")]
[Authorize]
public sealed class TemplatesController : BaseController
{
    private readonly ITemplateService _service;
    public TemplatesController(ITemplateService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] TemplateStatus? status, [FromQuery] string? category, CancellationToken ct) =>
        ToResponse(await _service.SearchAsync(keyword, status, category, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        ToResponse(await _service.GetByIdAsync(id, ct));

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct) =>
        ToResponse(await _service.GetByCodeAsync(code, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken ct) =>
        Created(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}/metadata")]
    public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateTemplateMetadataRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateMetadataAsync(id, request, ct));

    [HttpPut("{id:guid}/content")]
    public async Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateTemplateContentRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateContentAsync(id, request, ct));

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct) =>
        ToResponse(await _service.PublishAsync(id, ct));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) =>
        ToResponse(await _service.ArchiveAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));

    /// <summary>
    /// Parse DOCX, detect placeholder + return base64 — FE giữ trong state, gửi kèm khi save template.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB cap cho file DOCX.
    public async Task<IActionResult> ImportFromWord([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "FILE_EMPTY" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ToResponse(await _service.ImportFromWordAsync(ms.ToArray(), file.FileName, ct));
    }

    /// <summary>
    /// OnlyOffice Document Server fetch DOCX bytes của template qua endpoint này.
    /// AllowAnonymous: OnlyOffice DocServer chạy server-to-server không có user JWT — security
    /// nên via IP whitelist hoặc OnlyOffice JWT (chưa enable ở POC).
    /// </summary>
    [HttpGet("{id:guid}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadDocx(Guid id, CancellationToken ct)
    {
        var result = await _service.GetDocxBytesAsync(id, ct);
        if (result.IsFailure) return ToResponse(result);
        var bytes = result.Data!;
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileDownloadName: $"{id}.docx");
    }

    /// <summary>
    /// OnlyOffice Document Server callback endpoint — gọi khi user save document trong editor.
    /// DocServer POST JSON với status/url. status=2 (ready to save) hoặc 6 (force save).
    /// Spec: https://api.onlyoffice.com/editors/callback
    /// </summary>
    [HttpPost("{id:guid}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OnlyOfficeCallback(Guid id, [FromBody] OnlyOfficeCallbackPayload payload, CancellationToken ct)
    {
        // Status 2 = MustSave (closed by all users); 6 = MustForcesave (autosave/manual save).
        if (payload?.Status is 2 or 6 && !string.IsNullOrWhiteSpace(payload.Url))
        {
            using var http = new HttpClient();
            byte[] docxBytes = await http.GetByteArrayAsync(payload.Url, ct);
            var result = await _service.ReplaceDocxBytesAsync(id, docxBytes, ct);
            if (result.IsFailure)
            {
                // OnlyOffice docs: trả error != 0 để DocServer biết save fail.
                return Ok(new { error = 1 });
            }
        }
        // OnlyOffice required ack: { error: 0 }.
        return Ok(new { error = 0 });
    }
}

/// <summary>
/// Payload từ OnlyOffice Document Server callback. Spec: https://api.onlyoffice.com/editors/callback
/// </summary>
public sealed class OnlyOfficeCallbackPayload
{
    public int Status { get; set; }
    public string? Url { get; set; }
    public string? Key { get; set; }
    public string[]? Users { get; set; }
    public string? UserData { get; set; }
}
