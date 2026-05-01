using BB.Common;

namespace Workflow.Domain;

public sealed class WorkflowTransition : BaseEntity
{
    public Guid WorkflowId { get; private set; }
    public Guid? FromStatusId { get; private set; }   // null = global transition (any status)
    public Guid ToStatusId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ScreenId { get; private set; }
    public bool IsAutomatic { get; private set; }

    private readonly List<TransitionRule> _rules = new();
    private readonly List<TransitionValidator> _validators = new();
    private readonly List<TransitionPostFunction> _postFunctions = new();

    public IReadOnlyList<TransitionRule> Rules => _rules;
    public IReadOnlyList<TransitionValidator> Validators => _validators;
    public IReadOnlyList<TransitionPostFunction> PostFunctions => _postFunctions;

    private WorkflowTransition() { }

    internal WorkflowTransition(Guid workflowId, Guid? fromStatusId, Guid toStatusId, string name, Guid? screenId = null, bool isAutomatic = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.TransitionInvalid, WorkflowErrors.MsgTransitionInvalid);
        if (toStatusId == Guid.Empty)
            throw new DomainException(WorkflowErrors.TransitionInvalid, WorkflowErrors.MsgTransitionInvalid);
        if (fromStatusId == toStatusId)
            throw new DomainException(WorkflowErrors.TransitionInvalid, WorkflowErrors.MsgTransitionInvalid);

        WorkflowId = workflowId;
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
        Name = name.Trim();
        ScreenId = screenId;
        IsAutomatic = isAutomatic;
    }

    public bool IsGlobal => FromStatusId is null;

    public bool AppliesFrom(Guid currentStatusId) =>
        IsGlobal || FromStatusId == currentStatusId;

    internal void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.TransitionInvalid, WorkflowErrors.MsgTransitionInvalid);
        Name = name.Trim();
    }

    internal void SetScreen(Guid? screenId) => ScreenId = screenId;
    internal void SetAutomatic(bool isAutomatic) => IsAutomatic = isAutomatic;

    public TransitionRule AddRule(string typeKey, string configJson, int? order = null)
    {
        var rule = new TransitionRule(Id, typeKey, configJson, order ?? _rules.Count);
        _rules.Add(rule);
        return rule;
    }

    public TransitionValidator AddValidator(string typeKey, string configJson, int? order = null)
    {
        var v = new TransitionValidator(Id, typeKey, configJson, order ?? _validators.Count);
        _validators.Add(v);
        return v;
    }

    public TransitionPostFunction AddPostFunction(string typeKey, string configJson, int? order = null)
    {
        var pf = new TransitionPostFunction(Id, typeKey, configJson, order ?? _postFunctions.Count);
        _postFunctions.Add(pf);
        return pf;
    }

    public void RemoveStep(Guid stepId)
    {
        _rules.RemoveAll(r => r.Id == stepId);
        _validators.RemoveAll(r => r.Id == stepId);
        _postFunctions.RemoveAll(r => r.Id == stepId);
    }
}
