using System.Text.RegularExpressions;
using BB.Common;
using CustomField.Domain.Events;

namespace CustomField.Domain;

public sealed class CustomField : AggregateRoot, ISoftDeletable
{
    private static readonly Regex KeyPattern = new("^[a-z][a-z0-9_]{2,49}$", RegexOptions.Compiled);

    public string Key { get; private set; } = string.Empty;       // immutable, dùng cho API/JQL
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CustomFieldType Type { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsSearchable { get; private set; }
    public string ConfigJson { get; private set; } = "{}";

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<CustomFieldOption> _options = new();
    public IReadOnlyList<CustomFieldOption> Options => _options;

    private readonly List<CustomFieldContext> _contexts = new();
    public IReadOnlyList<CustomFieldContext> Contexts => _contexts;

    private CustomField() { }

    public CustomField(string key, string name, CustomFieldType type, string? description = null, bool isSearchable = false, string? configJson = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(CustomFieldErrors.NameRequired, CustomFieldErrors.MsgNameRequired);
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException(CustomFieldErrors.KeyRequired, CustomFieldErrors.MsgKeyRequired);
        if (!KeyPattern.IsMatch(key))
            throw new DomainException(CustomFieldErrors.KeyInvalid, CustomFieldErrors.MsgKeyInvalid);

        Key = key.Trim().ToLowerInvariant();
        Name = name.Trim();
        Description = description;
        Type = type;
        IsSearchable = isSearchable;
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        IsSystem = isSystem;

        RaiseDomainEvent(new CustomFieldCreated(Id, Key, type));
    }

    public bool SupportsOptions =>
        Type is CustomFieldType.Select or CustomFieldType.MultiSelect or CustomFieldType.Cascading or CustomFieldType.Label;

    public CustomFieldOption AddOption(string value, string label, Guid? parentOptionId = null, int? order = null)
    {
        if (!SupportsOptions)
            throw new DomainException(CustomFieldErrors.OptionNotForType, CustomFieldErrors.MsgOptionNotForType);
        if (_options.Any(o => string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase) && o.ParentOptionId == parentOptionId))
            throw new DomainException(CustomFieldErrors.OptionDuplicated, CustomFieldErrors.MsgOptionDup);

        var opt = new CustomFieldOption(Id, value, label, order ?? _options.Count, parentOptionId);
        _options.Add(opt);
        return opt;
    }

    public void RemoveOption(Guid optionId)
    {
        var opt = _options.FirstOrDefault(o => o.Id == optionId)
                  ?? throw new DomainException(CustomFieldErrors.OptionNotFound, CustomFieldErrors.MsgOptionNotFound);
        _options.Remove(opt);
    }

    public void UpdateOption(Guid optionId, string value, string label, int order)
    {
        var opt = _options.FirstOrDefault(o => o.Id == optionId)
                  ?? throw new DomainException(CustomFieldErrors.OptionNotFound, CustomFieldErrors.MsgOptionNotFound);
        opt.Update(value, label, order);
    }

    public CustomFieldContext AddContext(string name, bool isGlobal, bool isRequired,
        string? defaultValueJson, IReadOnlyCollection<Guid>? projectIds = null, IReadOnlyCollection<Guid>? issueTypeIds = null)
    {
        var ctx = new CustomFieldContext(Id, name, isGlobal, isRequired, defaultValueJson, projectIds, issueTypeIds);
        _contexts.Add(ctx);
        return ctx;
    }

    public void RemoveContext(Guid contextId)
    {
        var ctx = _contexts.FirstOrDefault(c => c.Id == contextId)
                  ?? throw new DomainException(CustomFieldErrors.ContextNotFound, CustomFieldErrors.MsgContextNotFound);
        _contexts.Remove(ctx);
    }

    public CustomFieldContext? ResolveContext(Guid projectId, Guid issueTypeId) =>
        _contexts.FirstOrDefault(c => !c.IsGlobal && c.AppliesTo(projectId, issueTypeId))
        ?? _contexts.FirstOrDefault(c => c.IsGlobal && c.AppliesTo(projectId, issueTypeId));

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(CustomFieldErrors.NameRequired, CustomFieldErrors.MsgNameRequired);
        Name = name.Trim();
    }
    public void UpdateDescription(string? description) => Description = description;
    public void UpdateConfig(string configJson) => ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
    public void SetSearchable(bool searchable) => IsSearchable = searchable;

    public void EnsureCanDelete()
    {
        if (IsSystem)
            throw new DomainException(CustomFieldErrors.CannotDeleteSystem, CustomFieldErrors.MsgCannotDeleteSystem);
    }
}
