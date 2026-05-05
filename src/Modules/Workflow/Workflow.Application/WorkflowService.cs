using BB.Common;
using BB.Security;
using Microsoft.Extensions.Logging;
using Workflow.Application.Repositories;
using Workflow.Domain;

namespace Workflow.Application;

public sealed class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _repo;
    private readonly IWorkflowUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionChecker _permissions;
    private readonly IAuditLogger _audit;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IWorkflowRepository repo, IWorkflowUnitOfWork uow, ICurrentUser currentUser, IPermissionChecker permissions, IAuditLogger audit, ILogger<WorkflowService> logger)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Check workflow admin permission cho project-scoped workflow.
    /// Workflow template (ProjectId=null) bỏ qua check (chỉ dev/admin truy cập).
    /// </summary>
    private async Task<Result> EnsureWorkflowAdminAsync(Domain.Workflow w, CancellationToken ct)
    {
        if (w.ProjectId is null) return Result.Success(); // template — không có scope project
        return await _permissions.RequireProjectAsync(_currentUser.UserId, w.ProjectId.Value, PermissionKeys.ProjectAdminWorkflow, ct);
    }

    public async Task<Result<WorkflowDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(id, ct);
        return w is null
            ? Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found")
            : Result.Success(WorkflowMapper.ToDto(w));
    }

    public async Task<Result<IReadOnlyList<WorkflowDto>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var list = await _repo.ListByProjectAsync(projectId, ct);
        return Result.Success<IReadOnlyList<WorkflowDto>>(list.Select(WorkflowMapper.ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<WorkflowDto>>> ListTemplatesAsync(CancellationToken ct = default)
    {
        var list = await _repo.ListTemplatesAsync(ct);
        return Result.Success<IReadOnlyList<WorkflowDto>>(list.Select(WorkflowMapper.ToDto).ToList());
    }

    public async Task<Result<WorkflowDto>> CreateAsync(CreateWorkflowRequest request, CancellationToken ct = default)
    {
        // R2: tạo workflow cho project = workflow admin trên project đó.
        if (!request.IsTemplate && request.ProjectId is { } pid)
        {
            var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, pid, PermissionKeys.ProjectAdminWorkflow, ct);
            if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);
        }

        if (await _repo.KeyExistsAsync(request.ProjectId, request.Key, null, ct))
        {
            return Result.Failure<WorkflowDto>(
                ErrorType.Conflict, "workflow.key.duplicated",
                new[] { new ResultError("WORKFLOW_KEY_DUPLICATED", "workflow.key.duplicated", "key") });
        }

        var workflow = request.IsTemplate
            ? Domain.Workflow.CreateTemplate(request.Name, request.Key, request.Description)
            : Domain.Workflow.CreateForProject(
                request.ProjectId ?? throw new DomainException("WORKFLOW_PROJECT_REQUIRED", "workflow.project.required"),
                request.Name, request.Key, request.Description);

        await _repo.AddAsync(workflow, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Workflow created Id={Id} Key={Key}", workflow.Id, workflow.Key);
        await _audit.LogAsync(AuditActions.WorkflowCreated, "workflow", workflow.Id,
            new { workflow.Key, workflow.Name, workflow.ProjectId, workflow.IsTemplate }, ct);

        return Result.Success(WorkflowMapper.ToDto(workflow), "workflow.created.success", new { name = workflow.Name });
    }

    public async Task<Result<WorkflowDto>> UpdateAsync(Guid id, UpdateWorkflowRequest request, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(id, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.Rename(request.Name);
        w.UpdateDescription(request.Description);
        if (request.IsActive) w.Activate(); else w.Deactivate();
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.updated.success");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct);
        if (w is null) return Result.Failure(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return perm;

        _repo.Remove(w);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditActions.WorkflowDeleted, "workflow", w.Id, new { w.Key, w.Name, w.ProjectId }, ct);
        return Result.Success(messageKey: "workflow.deleted.success");
    }

    public async Task<Result<WorkflowDto>> AddStatusAsync(Guid workflowId, AddStatusRequest request, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.AddStatus(request.Name, request.Key, (StatusCategory)request.Category, request.Color, request.Order);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.status.added");
    }

    public async Task<Result<WorkflowDto>> RemoveStatusAsync(Guid workflowId, Guid statusId, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.RemoveStatus(statusId);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.status.removed");
    }

    public async Task<Result<WorkflowDto>> SetInitialStatusAsync(Guid workflowId, Guid statusId, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.SetInitialStatus(statusId);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.status.initial_set");
    }

    public async Task<Result<WorkflowDto>> AddTransitionAsync(Guid workflowId, AddTransitionRequest request, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.AddTransition(request.FromStatusId, request.ToStatusId, request.Name, request.ScreenId, request.IsAutomatic);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.transition.added");
    }

    public async Task<Result<WorkflowDto>> RemoveTransitionAsync(Guid workflowId, Guid transitionId, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        w.RemoveTransition(transitionId);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.transition.removed");
    }

    public Task<Result<WorkflowDto>> AddRuleAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default) =>
        AddStepAsync(workflowId, transitionId, request, StepKind.Rule, ct);

    public Task<Result<WorkflowDto>> AddValidatorAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default) =>
        AddStepAsync(workflowId, transitionId, request, StepKind.Validator, ct);

    public Task<Result<WorkflowDto>> AddPostFunctionAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, CancellationToken ct = default) =>
        AddStepAsync(workflowId, transitionId, request, StepKind.PostFunction, ct);

    public async Task<Result<WorkflowDto>> RemoveTransitionStepAsync(Guid workflowId, Guid transitionId, Guid stepId, CancellationToken ct = default)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        var t = w.FindTransition(transitionId);
        if (t is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, WorkflowErrors.MsgTransitionNotFound);

        t.RemoveStep(stepId);
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.transition.step.removed");
    }

    private async Task<Result<WorkflowDto>> AddStepAsync(Guid workflowId, Guid transitionId, AddTransitionStepRequest request, StepKind kind, CancellationToken ct)
    {
        var w = await _repo.GetWithDetailsAsync(workflowId, ct);
        if (w is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, "workflow.not_found");

        var perm = await EnsureWorkflowAdminAsync(w, ct);
        if (perm.IsFailure) return Result.Failure<WorkflowDto>(perm);

        var t = w.FindTransition(transitionId);
        if (t is null) return Result.Failure<WorkflowDto>(ErrorType.NotFound, WorkflowErrors.MsgTransitionNotFound);

        switch (kind)
        {
            case StepKind.Rule: t.AddRule(request.TypeKey, request.ConfigJson, request.Order); break;
            case StepKind.Validator: t.AddValidator(request.TypeKey, request.ConfigJson, request.Order); break;
            case StepKind.PostFunction: t.AddPostFunction(request.TypeKey, request.ConfigJson, request.Order); break;
        }
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(WorkflowMapper.ToDto(w), "workflow.transition.step.added");
    }

    private enum StepKind { Rule, Validator, PostFunction }
}
