using System.Text.RegularExpressions;
using BB.Common;
using Workflow.Domain.Events;

namespace Workflow.Domain;

public sealed class Workflow : AggregateRoot, ISoftDeletable
{
    private static readonly Regex KeyPattern = new("^[A-Z][A-Z0-9_]{2,49}$", RegexOptions.Compiled);

    public Guid? ProjectId { get; private set; }       // null = global template
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsTemplate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid InitialStatusId { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<WorkflowStatus> _statuses = new();
    private readonly List<WorkflowTransition> _transitions = new();

    public IReadOnlyList<WorkflowStatus> Statuses => _statuses;
    public IReadOnlyList<WorkflowTransition> Transitions => _transitions;

    private Workflow() { }

    private Workflow(Guid? projectId, string name, string key, bool isTemplate, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.NameRequired, WorkflowErrors.MsgNameRequired);
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException(WorkflowErrors.KeyRequired, WorkflowErrors.MsgKeyRequired);
        if (!KeyPattern.IsMatch(key))
            throw new DomainException(WorkflowErrors.KeyInvalid, WorkflowErrors.MsgKeyInvalid);

        ProjectId = projectId;
        Name = name.Trim();
        Key = key.Trim().ToUpperInvariant();
        Description = description;
        IsTemplate = isTemplate;
    }

    public static Workflow CreateForProject(Guid projectId, string name, string key, string? description = null) =>
        new(projectId, name, key, isTemplate: false, description);

    public static Workflow CreateTemplate(string name, string key, string? description = null) =>
        new(projectId: null, name, key, isTemplate: true, description);

    public WorkflowStatus AddStatus(string name, string key, StatusCategory category, string? color = null, int? order = null)
    {
        if (_statuses.Any(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException(WorkflowErrors.StatusKeyDuplicated, WorkflowErrors.MsgStatusKeyDup);

        var s = new WorkflowStatus(Id, name, key, category, color, order ?? _statuses.Count);
        _statuses.Add(s);
        if (InitialStatusId == Guid.Empty) InitialStatusId = s.Id;
        return s;
    }

    public void RemoveStatus(Guid statusId)
    {
        if (_transitions.Any(t => t.FromStatusId == statusId || t.ToStatusId == statusId))
            throw new DomainException(WorkflowErrors.StatusInUse, WorkflowErrors.MsgStatusInUse);

        var status = _statuses.FirstOrDefault(s => s.Id == statusId)
            ?? throw new DomainException(WorkflowErrors.StatusNotFound, WorkflowErrors.MsgStatusNotFound);

        if (InitialStatusId == statusId)
            throw new DomainException(WorkflowErrors.StatusInUse, WorkflowErrors.MsgStatusInUse);

        _statuses.Remove(status);
    }

    public void SetInitialStatus(Guid statusId)
    {
        if (_statuses.All(s => s.Id != statusId))
            throw new DomainException(WorkflowErrors.StatusNotFound, WorkflowErrors.MsgStatusNotFound);
        InitialStatusId = statusId;
    }

    public WorkflowTransition AddTransition(Guid? fromStatusId, Guid toStatusId, string name, Guid? screenId = null, bool isAutomatic = false)
    {
        if (toStatusId == Guid.Empty || _statuses.All(s => s.Id != toStatusId))
            throw new DomainException(WorkflowErrors.StatusNotFound, WorkflowErrors.MsgStatusNotFound);
        if (fromStatusId is { } from && _statuses.All(s => s.Id != from))
            throw new DomainException(WorkflowErrors.StatusNotFound, WorkflowErrors.MsgStatusNotFound);
        if (_transitions.Any(t => t.FromStatusId == fromStatusId && t.ToStatusId == toStatusId))
            throw new DomainException(WorkflowErrors.TransitionDuplicated, WorkflowErrors.MsgTransitionDup);

        var t = new WorkflowTransition(Id, fromStatusId, toStatusId, name, screenId, isAutomatic);
        _transitions.Add(t);
        return t;
    }

    public void RemoveTransition(Guid transitionId)
    {
        var t = _transitions.FirstOrDefault(x => x.Id == transitionId)
            ?? throw new DomainException(WorkflowErrors.TransitionNotFound, WorkflowErrors.MsgTransitionNotFound);
        _transitions.Remove(t);
    }

    public WorkflowTransition? FindTransition(Guid transitionId) =>
        _transitions.FirstOrDefault(t => t.Id == transitionId);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.NameRequired, WorkflowErrors.MsgNameRequired);
        Name = name.Trim();
    }
    public void UpdateDescription(string? description) => Description = description;

    /// <summary>
    /// Validate snapshot trước khi publish workflow: phải có ≥ 1 status và InitialStatus hợp lệ.
    /// </summary>
    public void EnsurePublishable()
    {
        if (_statuses.Count == 0 || InitialStatusId == Guid.Empty)
            throw new DomainException(WorkflowErrors.MustHaveInitialStatus, WorkflowErrors.MsgMustHaveInitial);
    }

    /// <summary>
    /// Domain-level: ghi nhận sự kiện issue đã transition. Engine ở Application gọi.
    /// </summary>
    public void RaiseIssueTransitioned(Guid issueId, Guid? fromStatusId, Guid toStatusId, Guid transitionId, string changedBy)
    {
        RaiseDomainEvent(new IssueTransitioned(issueId, Id, fromStatusId, toStatusId, transitionId, changedBy));
    }
}
