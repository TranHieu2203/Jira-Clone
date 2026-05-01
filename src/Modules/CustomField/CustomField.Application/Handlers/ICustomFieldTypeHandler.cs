using System.Text.Json;
using BB.Common;
using CustomField.Domain;

namespace CustomField.Application.Handlers;

/// <summary>Strategy: validate + normalize + project + index theo từng kiểu field.</summary>
public interface ICustomFieldTypeHandler
{
    CustomFieldType Type { get; }

    /// <summary>Kiểm tra value có hợp lệ với definition + options không.</summary>
    Task<Result> ValidateAsync(Domain.CustomField def, JsonElement value, CancellationToken ct = default);

    /// <summary>Chuẩn hoá value (trim, lower, sort) trước khi lưu.</summary>
    JsonElement Normalize(Domain.CustomField def, JsonElement input);

    /// <summary>Tách indexed columns. Chỉ điền nếu IsSearchable=true.</summary>
    (string? indexedString, decimal? indexedNumber, DateTimeOffset? indexedDate) Index(JsonElement value);

    /// <summary>Trả về object FE-friendly (Issue API trả về client).</summary>
    object? ProjectForApi(JsonElement value);
}

public interface ICustomFieldTypeHandlerRegistry
{
    ICustomFieldTypeHandler? Find(CustomFieldType type);
    ICustomFieldTypeHandler Require(CustomFieldType type);
}

public sealed class CustomFieldTypeHandlerRegistry : ICustomFieldTypeHandlerRegistry
{
    private readonly Dictionary<CustomFieldType, ICustomFieldTypeHandler> _map;

    public CustomFieldTypeHandlerRegistry(IEnumerable<ICustomFieldTypeHandler> handlers)
    {
        _map = handlers.ToDictionary(h => h.Type);
    }

    public ICustomFieldTypeHandler? Find(CustomFieldType type) => _map.GetValueOrDefault(type);

    public ICustomFieldTypeHandler Require(CustomFieldType type) =>
        _map.GetValueOrDefault(type)
        ?? throw new DomainException(CustomFieldErrors.TypeHandlerMissing, CustomFieldErrors.MsgTypeHandlerMissing);
}
