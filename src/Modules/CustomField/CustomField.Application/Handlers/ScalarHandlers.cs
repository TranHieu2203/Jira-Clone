using System.Globalization;
using System.Text.Json;
using BB.Common;
using CustomField.Domain;

namespace CustomField.Application.Handlers;

internal static class HandlerHelpers
{
    public static JsonElement Wrap(object? value)
    {
        if (value is null) return JsonDocument.Parse("null").RootElement.Clone();
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public static JsonElement? Unwrap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("v", out var v))
            return v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : v;
        return value;
    }

    public static Result Invalid(string code, string messageKey, string? field = null) =>
        Result.Failure(ErrorType.Validation, "field.value.invalid",
            new[] { new ResultError(code, messageKey, Field: field) });
}

public sealed class TextHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Text;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.String)
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input)
    {
        var v = HandlerHelpers.Unwrap(input);
        if (v is null) return HandlerHelpers.Wrap(new { v = (string?)null });
        return HandlerHelpers.Wrap(new { v = v.Value.GetString()?.Trim() });
    }

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        return (v?.GetString(), null, null);
    }

    public object? ProjectForApi(JsonElement value) => HandlerHelpers.Unwrap(value)?.GetString();
}

public sealed class TextAreaHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.TextArea;
    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default) =>
        new TextHandler().ValidateAsync(def, value, ct);
    public JsonElement Normalize(Domain.CustomField def, JsonElement input)
    {
        var v = HandlerHelpers.Unwrap(input);
        return HandlerHelpers.Wrap(new { v = v?.GetString() });
    }
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) => (null, null, null);
    public object? ProjectForApi(JsonElement value) => HandlerHelpers.Unwrap(value)?.GetString();
}

public sealed class NumberHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Number;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.Number || !v.Value.TryGetInt64(out _))
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Number) return (null, null, null);
        return (null, v.Value.GetInt64(), null);
    }

    public object? ProjectForApi(JsonElement value) =>
        HandlerHelpers.Unwrap(value) is { ValueKind: JsonValueKind.Number } n ? n.GetInt64() : (long?)null;
}

public sealed class DecimalHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Decimal;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.Number)
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.Number) return (null, null, null);
        return (null, v.Value.GetDecimal(), null);
    }

    public object? ProjectForApi(JsonElement value) =>
        HandlerHelpers.Unwrap(value) is { ValueKind: JsonValueKind.Number } n ? n.GetDecimal() : (decimal?)null;
}

public sealed class DateHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Date;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.String ||
            !DateTime.TryParse(v.Value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind != JsonValueKind.String) return (null, null, null);
        return DateTimeOffset.TryParse(v.Value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
            ? (null, null, dt) : (null, null, null);
    }

    public object? ProjectForApi(JsonElement value) =>
        HandlerHelpers.Unwrap(value) is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;
}

public sealed class DateTimeHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.DateTime;
    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default) =>
        new DateHandler().ValidateAsync(def, value, ct);
    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) =>
        new DateHandler().Index(value);
    public object? ProjectForApi(JsonElement value) =>
        HandlerHelpers.Unwrap(value) is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;
}

public sealed class CheckboxHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Checkbox;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null))
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;

    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        return v?.ValueKind == JsonValueKind.True ? ("true", null, null)
             : v?.ValueKind == JsonValueKind.False ? ("false", null, null)
             : (null, null, null);
    }

    public object? ProjectForApi(JsonElement value)
    {
        var v = HandlerHelpers.Unwrap(value);
        return v?.ValueKind == JsonValueKind.True ? true
             : v?.ValueKind == JsonValueKind.False ? false : (bool?)null;
    }
}

public sealed class UrlHandler : ICustomFieldTypeHandler
{
    public CustomFieldType Type => CustomFieldType.Url;

    public Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default)
    {
        var v = HandlerHelpers.Unwrap(value);
        if (v is null || v.Value.ValueKind == JsonValueKind.Null) return Task.FromResult(Result.Success());
        if (v.Value.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(v.Value.GetString(), UriKind.Absolute, out _))
            return Task.FromResult(HandlerHelpers.Invalid(CustomFieldErrors.ValueInvalid, CustomFieldErrors.MsgValueInvalid, def.Key));
        return Task.FromResult(Result.Success());
    }

    public JsonElement Normalize(Domain.CustomField def, JsonElement input) => input;
    public (string?, decimal?, DateTimeOffset?) Index(JsonElement value) =>
        (HandlerHelpers.Unwrap(value)?.GetString(), null, null);
    public object? ProjectForApi(JsonElement value) => HandlerHelpers.Unwrap(value)?.GetString();
}
