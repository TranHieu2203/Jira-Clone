using BB.Common;

namespace Workflow.Application;

public interface IWorkflowService
{
    Task<Result<WorkflowDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkflowDto>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkflowDto>>> ListTemplatesAsync(CancellationToken ct = default);
    Task<Result<WorkflowDto>> CreateAsync(CreateWorkflowRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> UpdateAsync(Guid id, UpdateWorkflowRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<WorkflowDto>> AddStatusAsync(Guid workflowId, AddStatusRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> RemoveStatusAsync(Guid workflowId, Guid statusId, CancellationToken ct = default);
    Task<Result<WorkflowDto>> SetInitialStatusAsync(Guid workflowId, Guid statusId, CancellationToken ct = default);

    Task<Result<WorkflowDto>> AddTransitionAsync(Guid workflowId, AddTransitionRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> RemoveTransitionAsync(Guid workflowId, Guid transitionId, CancellationToken ct = default);

    Task<Result<WorkflowDto>> AddRuleAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> AddValidatorAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> AddPostFunctionAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default);
    Task<Result<WorkflowDto>> RemoveTransitionStepAsync(Guid workflowId, Guid transitionId, Guid stepId, CancellationToken ct = default);
}
