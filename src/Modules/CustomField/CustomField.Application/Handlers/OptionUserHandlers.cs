using System.Text.Json;
using BB.Common;
using CustomField.Domain;

namespace CustomField.Application.Handlers;

public sealed class SelectHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Select;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.String)
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));

        var raw = v.Value.GetString();
        if (string.IsNullOrEmpty(raw)) return Task.FromResult(Result.Success());

        // Lưu theo option-id; chấp nhận cả value khi option-id parse fail.
        var found = Guid.TryParse(raw, out var oid)
            ? def.Options.Any(o => o.Id == oid)
            : def.Options.Any(o => string.Equals(o.Value, raw, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(found
            ? Result.Success()
            : HandlerHelpers.Invalid(CustomFieldErrors.OptionNotFound, CustomFieldErrors.MsgOptionNotFound, def.Key));
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) =>
        (HandlerHelpers.Unwrap(value)?.GetString(), null, null);

    public object? ProjectForApi(JsonElement value) => HandlerHelpers.Unwrap(value)?.GetString();
}

public sealed class MultiSelectHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.MultiSelect;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.Array)
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));

        foreach (var item in v.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) continue;
            var raw = item.GetString() ?? string.Empty;
            var found = Guid.TryParse(raw, out var oid)
                ? def.Options.Any(o => o.Id == oid)
                : def.Options.Any(o => string.Equals(o.Value, raw, StringComparison.OrdinalIgnoreCase));
            if (!found)
                return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.OptionNotFound, CustomFieldErrors.MsgOptionNotFound, def.Key));
        }
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Array) return (null, null, null);
        var arr = v.Value.EnumerateArray()
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .OrderBy(s => s);
        return (string.Join(",", arr), null, null);
    }

    public object? ProjectForApi(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
        return v.Value.EnumerateArray().Select(e => e.GetString()).ToArray();
    }
}

public sealed class UserHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.User;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.String || !Guid.TryParse(v.Value.GetString(), out _))
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) =>
        (HandlerHelpers.Unwrap(value)?.GetString(), null, null);
    public object? ProjectForApi(JsonElement value) => HandlerHelpers.Unwrap(value)?.GetString();
}

public sealed class UserMultiHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.UserMulti;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.Array)
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));

        foreach (var item in v.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out _))
                return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        }
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Array) return (null, null, null);
        var arr = v.Value.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s);
        return (string.Join(",", arr), null, null);
    }

    public object? ProjectForApi(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
        return v.Value.EnumerateArray().Select(e => e.GetString()).ToArray();
    }
}

public sealed class LabelHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Label;
    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default) =>
        new MultiSelectHandler().ValidateAsync(def, value, ct);
    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) =>
        new MultiSelectHandler().Index(value);
    public object? ProjectForApi(JsonElement value) =>
        new MultiSelectHandler().ProjectForApi(value);
}
