using System.Text.Json;
using BB.Common;
using CustomField.Application.Handlers;
using CustomField.Application.Repositories;
using CustomField.Domain;
using Microsoft.Extensions.Logging;

namespace CustomField.Application;

public sealed class IssueFieldValueService : IIssueFieldValueService
{
    private readonly ICustomFieldRepository _fieldRepo;
    private readonly IIssueFieldValueRepository _valueRepo;
    private readonly ICustomFieldTypeHandlerRegistry _registry;
    private readonly ICustomFieldUnitOfWork _uow;
    private readonly ILogger<IssueFieldValueService> _logger;

    public IssueFieldValueService(
        ICustomFieldRepository fieldRepo,
        IIssueFieldValueRepository valueRepo,
        ICustomFieldTypeHandlerRegistry registry,
        ICustomFieldUnitOfWork uow,
        ILogger<IssueFieldValueService> logger)
    {
        _fieldRepo = fieldRepo;
        _valueRepo = valueRepo;
        _registry = registry;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<IssueFieldValueDto>>> ListByIssueAsync(Guid issueId, CancellationToken ct = default)
    {
        var values = await _valueRepo.ListByIssueAsync(issueId, ct);
        if (values.Count == 0)
            return Result.Success<IReadOnlyList<IssueFieldValueDto>>(Array.Empty<IssueFieldValueDto>());

        var fields = await _fieldRepo.ListAllAsync(ct);
        var byId = fields.ToDictionary(f => f.Id);

        var dtos = new List<IssueFieldValueDto>(values.Count);
        foreach (var v in values)
        {
            if (!byId.TryGetValue(v.CustomFieldId, out var field)) continue;
            var element = JsonDocument.Parse(v.ValueJson).RootElement.Clone();
            dtos.Add(new IssueFieldValueDto(field.Id, field.Key, (int)field.Type, element));
        }
        return Result.Success<IReadOnlyList<IssueFieldValueDto>>(dtos);
    }

    public async Task<Result> SetValuesAsync(SetIssueFieldValuesRequest request, CancellationToken ct = default)
    {
        // 1. Resolve các field áp dụng cho project + issueType
        var applicableFields = await _fieldRepo.ResolveForAsync(request.ProjectId, request.IssueTypeId, ct);
        var applicableById = applicableFields.ToDictionary(f => f.Id);

        // 2. Required check: field IsRequired theo context phải có value (đã có hoặc đang set)
        var inputsByFieldId = (request.Values ?? new()).ToDictionary(v => v.CustomFieldId, v => v.Value);
        var existing = await _valueRepo.ListByIssueAsync(request.IssueId, ct);
        var existingByField = existing.ToDictionary(e => e.CustomFieldId);

        var errors = new List<ResultError>();
        foreach (var field in applicableFields)
        {
            var ctx = field.ResolveContext(request.ProjectId, request.IssueTypeId);
            if (ctx is null || !ctx.IsRequired) continue;

            var hasInput = inputsByFieldId.TryGetValue(field.Id, out var inputVal) && IsNonEmpty(inputVal);
            var hasExisting = existingByField.ContainsKey(field.Id);
            if (!hasInput && !hasExisting)
                errors.Add(new ResultError(CustomFieldErrors.ValueRequired, "validation.required", Field: field.Key));
        }
        if (errors.Count > 0)
            return Result.Failure(ErrorType.Validation, "field.value.required", errors);

        // 3. Validate + apply each input
        foreach (var input in request.Values ?? new())
        {
            if (!applicableById.TryGetValue(input.CustomFieldId, out var def))
                continue; // field không áp dụng cho project/issueType này — silent skip

            var handler = _registry.Find(def.Type);
            if (handler is null)
            {
                errors.Add(new ResultError(CustomFieldErrors.TypeHandlerMissing, CustomFieldErrors.MsgTypeHandlerMissing, Field: def.Key));
                continue;
            }

            var validate = await handler.ValidateAsync(def, input.Value, ct);
            if (!validate.IsSuccess) errors.AddRange(validate.Errors);
        }
        if (errors.Count > 0)
            return Result.Failure(ErrorType.Validation, "field.value.invalid", errors);

        foreach (var input in request.Values ?? new())
        {
            if (!applicableById.TryGetValue(input.CustomFieldId, out var def)) continue;
            var handler = _registry.Require(def.Type);

            var normalized = handler.Normalize(def, input.Value);
            var (idxStr, idxNum, idxDate) = def.IsSearchable ? handler.Index(normalized) : (null, null, null);
            var json = JsonSerializer.Serialize(new { v = normalized });

            if (existingByField.TryGetValue(def.Id, out var existingValue))
            {
                existingValue.UpdateValue(json, idxStr, idxNum, idxDate);
                _valueRepo.Update(existingValue);
            }
            else
            {
                var newValue = new IssueFieldValue(request.IssueId, def.Id, json, idxStr, idxNum, idxDate);
                await _valueRepo.AddAsync(newValue, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Issue {IssueId} field values updated ({Count})", request.IssueId, request.Values?.Count ?? 0);
        return Result.Success(messageKey: "field.value.saved");
    }

    public async Task<Result> ClearForIssueAsync(Guid issueId, CancellationToken ct = default)
    {
        await _valueRepo.RemoveAllForIssueAsync(issueId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(messageKey: "field.value.cleared");
    }

    private static bool IsNonEmpty(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => false,
        JsonValueKind.String => !string.IsNullOrWhiteSpace(v.GetString()),
        JsonValueKind.Array => v.GetArrayLength() > 0,
        _ => true
    };
}
