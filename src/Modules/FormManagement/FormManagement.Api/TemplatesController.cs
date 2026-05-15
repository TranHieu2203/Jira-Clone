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
    private readonly IDocumentConversionService _conversion;
    public TemplatesController(ITemplateService service, IDocumentConversionService conversion)
    {
        _service = service;
        _conversion = conversion;
    }

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
    /// <summary>
    /// Admin endpoint: wrap plain text «VALUE» trong current template bytes thành real MERGEFIELD.
    /// Hữu ích để normalize template đã lưu từ trước khi BE wrap-logic được thêm.
    /// </summary>
    [HttpPost("{id:guid}/normalize-fields")]
    public async Task<IActionResult> NormalizeFields(Guid id, CancellationToken ct)
    {
        var bytesResult = await _service.GetDocxBytesAsync(id, ct);
        if (bytesResult.IsFailure) return ToResponse(bytesResult);
        var wrapped = _conversion.WrapGuillemetsAsMergeFields(bytesResult.Data!);
        var saveResult = await _service.ReplaceDocxBytesAsync(id, wrapped, ct);
        return ToResponse(saveResult);
    }

    /// <summary>
    /// Trigger DocServer force-save cho 1 docKey đang mở. BE proxy lên DS CommandService
    /// vì DS chạy cùng docker network, FE không access trực tiếp được (cross-origin + JWT
    /// inside container).
    ///
    /// Flow:
    /// 1. FE click "Lưu template" → POST endpoint này với docKey hiện tại.
    /// 2. BE POST http://onlyoffice/coauthoring/CommandService.ashx {c:"forcesave", key}.
    /// 3. DS nhận → flush cached doc → fire callback status=6 (MustForcesave) tới
    ///    /callback endpoint → BE Wrap + persist DB (đúng cùng flow như Office Save).
    /// 4. FE poll /templates/{id} để detect version bump → gọi docEditor.refreshFile()
    ///    reload in-place (không unmount React → tránh removeChild crash).
    ///
    /// DS response: { "error": 0 } nếu OK, hoặc { "error": N } với N là mã lỗi.
    /// </summary>
    [HttpPost("{id:guid}/trigger-save")]
    public async Task<IActionResult> TriggerSave(Guid id, [FromBody] TriggerSavePayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload?.DocKey))
            return BadRequest(new { error = "DOC_KEY_REQUIRED" });

        // DS hostname inside docker network. Hard-code service name `onlyoffice` — match
        // docker-compose.dev.yml. OnlyOffice CommandService.ashx ở /coauthoring/CommandService.ashx
        // (port 80 mặc định của nginx trong DS container).
        var dsCommandUrl = "http://onlyoffice/coauthoring/CommandService.ashx";

        var bodyJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            c = "forcesave",
            key = payload.DocKey
        });

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var resp = await http.PostAsync(
                dsCommandUrl,
                new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"),
                ct);
            var respBody = await resp.Content.ReadAsStringAsync(ct);
            // DS trả { "error": 0 } khi accepted (vẫn async — callback sẽ fire sau).
            // error 1 = key not found (doc chưa mở session), 4 = no changes (already saved).
            return Ok(new { dsStatus = (int)resp.StatusCode, dsBody = respBody });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(502, new { error = "DS_UNREACHABLE", detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OnlyOfficeCallback(Guid id, [FromBody] OnlyOfficeCallbackPayload payload, CancellationToken ct)
    {
        // Status 2 = MustSave (closed by all users); 6 = MustForcesave (autosave/manual save).
        if (payload?.Status is 2 or 6 && !string.IsNullOrWhiteSpace(payload.Url))
        {
            // DocServer trả payload.Url dạng "http://localhost:8080/cache/files/..." — localhost từ
            // perspective của DocServer container. BE container không reach được localhost:8080
            // → phải rewrite về docker service name "onlyoffice" (port 80 internal).
            var fetchUrl = payload.Url;
            if (fetchUrl.StartsWith("http://localhost:8080", StringComparison.OrdinalIgnoreCase))
            {
                fetchUrl = "http://onlyoffice" + fetchUrl.Substring("http://localhost:8080".Length);
            }
            else if (fetchUrl.StartsWith("https://localhost:8080", StringComparison.OrdinalIgnoreCase))
            {
                fetchUrl = "http://onlyoffice" + fetchUrl.Substring("https://localhost:8080".Length);
            }

            using var http = new HttpClient();
            try
            {
                byte[] docxBytes = await http.GetByteArrayAsync(fetchUrl, ct);
                // Wrap «LABEL» thành real OOXML MERGEFIELD trước khi persist. Lý do: OnlyOffice
                // plugin sandbox không tạo được persist-able MERGEFIELD (range.AddField log ok
                // nhưng serialize ra plain text). BE post-process bù — lần sau editor mở doc sẽ
                // render field shading + Alt+F9 toggle, đồng thời SubmissionPage detect đầy đủ
                // via ExtractUsedFields (cả MERGEFIELD instrText lẫn plain text fallback).
                docxBytes = _conversion.WrapGuillemetsAsMergeFields(docxBytes);
                var result = await _service.ReplaceDocxBytesAsync(id, docxBytes, ct);
                if (result.IsFailure)
                {
                    return Ok(new { error = 1 });
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Log + báo DocServer save fail. DocServer sẽ retry vài lần.
                System.Diagnostics.Debug.WriteLine($"OO callback fetch failed url={fetchUrl} err={ex.Message}");
                return Ok(new { error = 1 });
            }
        }
        // OnlyOffice required ack: { error: 0 }.
        return Ok(new { error = 0 });
    }
}

/// <summary>FE → BE payload cho /trigger-save: chỉ cần docKey hiện tại của editor session.</summary>
public sealed class TriggerSavePayload
{
    public string? DocKey { get; set; }
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
