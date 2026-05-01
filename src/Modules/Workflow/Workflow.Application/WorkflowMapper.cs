using Workflow.Domain;

namespace Workflow.Application;

internal static class WorkflowMapper
{
    public static WorkflowDto ToDto(Domain.Workflow w) => new(
        w.Id, w.ProjectId, w.Name, w.Key, w.Description, w.IsTemplate, w.IsActive, w.InitialStatusId,
        w.Statuses.OrderBy(s => s.Order).Select(ToDto).ToList(),
        w.Transitions.Select(ToDto).ToList());

    public static WorkflowStatusDto ToDto(WorkflowStatus s) =>
        new(s.Id, s.Name, s.Key, (int)s.Category, s.Color, s.Order, s.IsFinal);

    public static WorkflowTransitionDto ToDto(WorkflowTransition t) => new(
        t.Id, t.FromStatusId, t.ToStatusId, t.Name, t.ScreenId, t.IsAutomatic,
        t.Rules.OrderBy(r => r.Order).Select(ToDto).ToList(),
        t.Validators.OrderBy(r => r.Order).Select(ToDto).ToList(),
        t.PostFunctions.OrderBy(r => r.Order).Select(ToDto).ToList());

    public static TransitionStepDto ToDto(TransitionStep s) =>
        new(s.Id, s.TypeKey, s.ConfigJson, s.Order);
}
