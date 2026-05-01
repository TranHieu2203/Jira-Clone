using BB.Common;
using Workflow.Application.Repositories;

namespace Workflow.Application;

public sealed class WorkflowResolver : IWorkflowResolver
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IWorkflowSchemeRepository _schemeRepo;

    public WorkflowResolver(IWorkflowRepository workflowRepo, IWorkflowSchemeRepository schemeRepo)
    {
        _workflowRepo = workflowRepo;
        _schemeRepo = schemeRepo;
    }

    public async Task<Result<WorkflowResolution>> ResolveForIssueAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default)
    {
        var scheme = await _schemeRepo.GetByProjectAsync(projectId, ct);
        if (scheme is null)
            return Result.Failure<WorkflowResolution>(ErrorType.NotFound, "workflow.scheme.not_found");

        var workflowId = scheme.Resolve(issueTypeId);
        var workflow = await _workflowRepo.GetWithDetailsAsync(workflowId, ct);
        if (workflow is null)
            return Result.Failure<WorkflowResolution>(ErrorType.NotFound, "workflow.not_found");

        if (workflow.InitialStatusId == Guid.Empty)
            return Result.Failure<WorkflowResolution>(ErrorType.Validation, "workflow.must_have_initial_status");

        return Result.Success(new WorkflowResolution(workflow.Id, workflow.InitialStatusId));
    }
}
