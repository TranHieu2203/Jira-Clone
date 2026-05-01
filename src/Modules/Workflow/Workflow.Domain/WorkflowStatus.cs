using BB.Common;

namespace Workflow.Domain;

public sealed class WorkflowStatus : BaseEntity
{
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public StatusCategory Category { get; private set; }
    public string? Color { get; private set; }
    public int Order { get; private set; }
    public bool IsFinal { get; private set; }

    private WorkflowStatus() { }

    internal WorkflowStatus(Guid workflowId, string name, string key, StatusCategory category, string? color, int order, bool isFinal = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.NameRequired, WorkflowErrors.MsgNameRequired);
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException(WorkflowErrors.KeyRequired, WorkflowErrors.MsgKeyRequired);

        WorkflowId = workflowId;
        Name = name.Trim();
        Key = key.Trim().ToUpperInvariant();
        Category = category;
        Color = color;
        Order = order;
        IsFinal = isFinal || category == StatusCategory.Done;
    }

    internal void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(WorkflowErrors.NameRequired, WorkflowErrors.MsgNameRequired);
        Name = name.Trim();
    }

    internal void SetOrder(int order) => Order = order;
    internal void SetCategory(StatusCategory category)
    {
        Category = category;
        if (category == StatusCategory.Done) IsFinal = true;
    }
    internal void SetColor(string? color) => Color = color;
}
