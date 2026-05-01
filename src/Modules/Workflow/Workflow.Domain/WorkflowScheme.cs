using BB.Common;

namespace Workflow.Domain;

public sealed class WorkflowScheme : AggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid DefaultWorkflowId { get; private set; }

    private readonly List<WorkflowSchemeItem> _items = new();
    public IReadOnlyList<WorkflowSchemeItem> Items => _items;

    private WorkflowScheme() { }

    public WorkflowScheme(Guid projectId, string name, Guid defaultWorkflowId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.NameRequired, WorkflowErrors.MsgNameRequired);
        if (defaultWorkflowId == Guid.Empty)
            throw new DomainException(WorkflowErrors.MustHaveInitialStatus, WorkflowErrors.MsgMustHaveInitial);

        ProjectId = projectId;
        Name = name.Trim();
        DefaultWorkflowId = defaultWorkflowId;
    }

    public void MapIssueType(Guid issueTypeId, Guid workflowId)
    {
        var existing = _items.FirstOrDefault(i => i.IssueTypeId == issueTypeId);
        if (existing is null) _items.Add(new WorkflowSchemeItem(Id, issueTypeId, workflowId));
        else existing.SetWorkflow(workflowId);
    }

    public void UnmapIssueType(Guid issueTypeId) =>
        _items.RemoveAll(i => i.IssueTypeId == issueTypeId);

    public Guid Resolve(Guid issueTypeId) =>
        _items.FirstOrDefault(i => i.IssueTypeId == issueTypeId)?.WorkflowId ?? DefaultWorkflowId;

    public void ChangeDefault(Guid workflowId)
    {
        if (workflowId == Guid.Empty)
            throw new DomainException(WorkflowErrors.MustHaveInitialStatus, WorkflowErrors.MsgMustHaveInitial);
        DefaultWorkflowId = workflowId;
    }
}

public sealed class WorkflowSchemeItem : BaseEntity
{
    public Guid SchemeId { get; private set; }
    public Guid IssueTypeId { get; private set; }
    public Guid WorkflowId { get; private set; }

    private WorkflowSchemeItem() { }

    internal WorkflowSchemeItem(Guid schemeId, Guid issueTypeId, Guid workflowId)
    {
        SchemeId = schemeId;
        IssueTypeId = issueTypeId;
        WorkflowId = workflowId;
    }

    internal void SetWorkflow(Guid workflowId) => WorkflowId = workflowId;
}
