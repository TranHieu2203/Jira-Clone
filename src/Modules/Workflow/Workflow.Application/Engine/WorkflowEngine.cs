using System.Text.Json;
using BB.Common;
using BB.Security;
using Microsoft.Extensions.Logging;
using Workflow.Application.Repositories;
using Workflow.Domain;

namespace Workflow.Application.Engine;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly IWorkflowSchemeRepository _schemeRepo;
    private readonly IIssueStatusHistoryRepository _historyRepo;
    private readonly ITransitionStepRegistry _registry;
    private readonly IWorkflowUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IWorkflowRepository workflowRepo,
        IWorkflowSchemeRepository schemeRepo,
        IIssueStatusHistoryRepository historyRepo,
        ITransitionStepRegistry registry,
        IWorkflowUnitOfWork uow,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<WorkflowEngine> logger)
    {
        _workflowRepo = workflowRepo;
        _schemeRepo = schemeRepo;
        _historyRepo = historyRepo;
        _registry = registry;
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<TransitionOutcome>> TransitionAsync(
        Guid issueId,
        Guid projectId,
        Guid issueTypeId,
        Guid currentStatusId,
        Guid transitionId,
        IReadOnlyDictionary<string, JsonElement>? inputs = null,
        string? comment = null,
        CancellationToken ct = default)
    {
        var (workflow, error) = await ResolveWorkflowAsync(projectId, issueTypeId, ct);
        if (workflow is null) return Result.Failure<TransitionOutcome>(ErrorType.NotFound, error ?? "workflow.not_found");

        var transition = workflow.FindTransition(transitionId);
        if (transition is null)
            return Result.Failure<TransitionOutcome>(ErrorType.NotFound, WorkflowErrors.MsgTransitionNotFound);

        if (!transition.AppliesFrom(currentStatusId))
            return Result.Failure<TransitionOutcome>(ErrorType.Validation, WorkflowErrors.MsgTransitionInvalid);

        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Failure<TransitionOutcome>(ErrorType.Unauthorized, "auth.required");

        var ctx = new TransitionContext
        {
            IssueId = issueId,
            ProjectId = projectId,
            IssueTypeId = issueTypeId,
            CurrentUserId = _currentUser.UserId.Value,
            CurrentUserName = _currentUser.UserName ?? string.Empty,
            WorkflowId = workflow.Id,
            TransitionId = transition.Id,
            FromStatusId = currentStatusId,
            ToStatusId = transition.ToStatusId,
            Inputs = inputs ?? new Dictionary<string, JsonElement>(),
            Comment = comment
        };

        // 1. Rules — fail any → forbidden
        foreach (var rule in transition.Rules.OrderBy(r => r.Order))
        {
            var handler = _registry.FindRule(rule.TypeKey);
            if (handler is null)
            {
                _logger.LogWarning("Unknown transition rule {TypeKey}", rule.TypeKey);
                continue;
            }
            var pass = await handler.EvaluateAsync(ctx, ParseConfig(rule.ConfigJson), ct);
            if (!pass)
                return Result.Failure<TransitionOutcome>(
                    ErrorType.Forbidden, "workflow.transition.forbidden",
                    new[] { new ResultError("WORKFLOW_RULE_FAILED", $"workflow.rule.{rule.TypeKey.ToLowerInvariant()}.failed") });
        }

        // 2. Validators — collect errors
        var errors = new List<ResultError>();
        foreach (var validator in transition.Validators.OrderBy(v => v.Order))
        {
            var handler = _registry.FindValidator(validator.TypeKey);
            if (handler is null)
            {
                _logger.LogWarning("Unknown transition validator {TypeKey}", validator.TypeKey);
                continue;
            }
            var result = await handler.ValidateAsync(ctx, ParseConfig(validator.ConfigJson), ct);
            if (!result.IsSuccess) errors.AddRange(result.Errors);
        }
        if (errors.Count > 0)
            return Result.Failure<TransitionOutcome>(ErrorType.Validation, "workflow.transition.invalid", errors);

        // 3. Post-functions — accumulate field changes in ctx
        foreach (var pf in transition.PostFunctions.OrderBy(p => p.Order))
        {
            var handler = _registry.FindPostFunction(pf.TypeKey);
            if (handler is null)
            {
                _logger.LogWarning("Unknown post-function {TypeKey}", pf.TypeKey);
                continue;
            }
            await handler.ExecuteAsync(ctx, ParseConfig(pf.ConfigJson), ct);
        }

        // 4. History
        var history = new IssueStatusHistory(
            issueId, workflow.Id, currentStatusId, transition.ToStatusId, transition.Id,
            ctx.CurrentUserName, _clock.UtcNow, comment);
        await _historyRepo.AddAsync(history, ct);

        // 5. Domain event (dispatch sau SaveChanges)
        workflow.RaiseIssueTransitioned(issueId, currentStatusId, transition.ToStatusId, transition.Id, ctx.CurrentUserName);
        _workflowRepo.Update(workflow);

        await _uow.SaveChangesAsync(ct);

        return Result.Success(new TransitionOutcome(
            issueId, currentStatusId, transition.ToStatusId, transition.Id, ctx.FieldChanges));
    }

    public async Task<Result<IReadOnlyList<AvailableTransition>>> GetAvailableTransitionsAsync(
        Guid projectId,
        Guid issueTypeId,
        Guid currentStatusId,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var (workflow, error) = await ResolveWorkflowAsync(projectId, issueTypeId, ct);
        if (workflow is null)
            return Result.Failure<IReadOnlyList<AvailableTransition>>(ErrorType.NotFound, error ?? "workflow.not_found");

        var candidates = workflow.Transitions.Where(t => t.AppliesFrom(currentStatusId)).ToList();
        var available = new List<AvailableTransition>(candidates.Count);

        foreach (var t in candidates)
        {
            // Eval rules nhẹ (giả định không có inputs — chỉ context user/issue).
            var ctx = new TransitionContext
            {
                ProjectId = projectId,
                IssueTypeId = issueTypeId,
                CurrentUserId = currentUserId,
                WorkflowId = workflow.Id,
                TransitionId = t.Id,
                FromStatusId = currentStatusId,
                ToStatusId = t.ToStatusId
            };

            var ok = true;
            foreach (var rule in t.Rules)
            {
                var handler = _registry.FindRule(rule.TypeKey);
                if (handler is null) continue;
                if (!await handler.EvaluateAsync(ctx, ParseConfig(rule.ConfigJson), ct))
                {
                    ok = false; break;
                }
            }
            if (!ok) continue;

            var toStatus = workflow.Statuses.FirstOrDefault(s => s.Id == t.ToStatusId);
            available.Add(new AvailableTransition(t.Id, t.Name, t.ToStatusId, toStatus?.Name ?? string.Empty, t.ScreenId));
        }

        return Result.Success<IReadOnlyList<AvailableTransition>>(available);
    }

    private async Task<(Domain.Workflow? workflow, string? error)> ResolveWorkflowAsync(
        Guid projectId, Guid issueTypeId, CancellationToken ct)
    {
        var scheme = await _schemeRepo.GetByProjectAsync(projectId, ct);
        if (scheme is null) return (null, "workflow.scheme.not_found");

        var workflowId = scheme.Resolve(issueTypeId);
        var workflow = await _workflowRepo.GetWithDetailsAsync(workflowId, ct);
        return workflow is null ? (null, "workflow.not_found") : (workflow, null);
    }

    private static JsonElement ParseConfig(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? JsonDocument.Parse("{}").RootElement
            : JsonDocument.Parse(json).RootElement;
}
