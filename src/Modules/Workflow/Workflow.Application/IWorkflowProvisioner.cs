using BB.Common;

namespace Workflow.Application;

/// <summary>
/// Cấp workflow + scheme mặc định cho 1 project khi chưa có (idempotent).
/// Clone từ template "SOFTWARE_SIMPLE" sang project-scoped workflow,
/// rồi tạo WorkflowScheme với default = workflow vừa clone.
/// </summary>
public interface IWorkflowProvisioner
{
    Task<Result> EnsureForProjectAsync(Guid projectId, CancellationToken ct = default);
}
