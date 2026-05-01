using BB.Common;

namespace Workflow.Application;

/// <summary>
/// Cross-module contract: cho Issue module resolve workflow + initial status
/// theo (projectId, issueTypeId) mà không cần phụ thuộc Workflow.Infrastructure.
/// </summary>
public interface IWorkflowResolver
{
    Task<Result<WorkflowResolution>> ResolveForIssueAsync(Guid projectId, Guid issueTypeId, CancellationToken ct = default);
}

public sealed record WorkflowResolution(Guid WorkflowId, Guid InitialStatusId);
