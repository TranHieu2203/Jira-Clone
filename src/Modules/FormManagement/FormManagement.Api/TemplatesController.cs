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
    /// Import file .docx / Word XML → SFDT JSON + danh sách placeholder.
    /// Phase 2 trả 409 ConversionUnsupported (stub). Phase 6 sẽ thay bằng Syncfusion DocIO impl.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB cap cho file Word.
    public async Task<IActionResult> ImportFromWord([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "FILE_EMPTY" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ToResponse(await _service.ImportFromWordAsync(ms.ToArray(), file.FileName, ct));
    }
}
