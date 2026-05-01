using BB.Common;
using Microsoft.Extensions.Logging;
using Workflow.Application.Repositories;
using Workflow.Domain;

namespace Workflow.Application;

public sealed class WorkflowProvisioner : IWorkflowProvisioner
{
    private const string TemplateKey = "SOFTWARE_SIMPLE";

    private readonly IWorkflowRepository _workflowRepo;
    private readonly IWorkflowSchemeRepository _schemeRepo;
    private readonly IWorkflowUnitOfWork _uow;
    private readonly ILogger<WorkflowProvisioner> _logger;

    public WorkflowProvisioner(
        IWorkflowRepository workflowRepo,
        IWorkflowSchemeRepository schemeRepo,
        IWorkflowUnitOfWork uow,
        ILogger<WorkflowProvisioner> logger)
    {
        _workflowRepo = workflowRepo;
        _schemeRepo = schemeRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> EnsureForProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        // Idempotent: nếu đã có scheme thì bỏ qua.
        var existing = await _schemeRepo.GetByProjectAsync(projectId, ct);
        if (existing is not null) return Result.Success();

        // Tìm template
        var templates = await _workflowRepo.ListTemplatesAsync(ct);
        var template = templates.FirstOrDefault(t => t.Key == TemplateKey);
        if (template is null)
            return Result.Failure(ErrorType.NotFound, "workflow.template.not_found");

        // Cần load full template với statuses + transitions
        var fullTemplate = await _workflowRepo.GetWithDetailsAsync(template.Id, ct);
        if (fullTemplate is null)
            return Result.Failure(ErrorType.NotFound, "workflow.template.not_found");

        // Clone sang project-scoped workflow.
        var clone = Domain.Workflow.CreateForProject(
            projectId,
            fullTemplate.Name,
            $"PROJ_{projectId.ToString("N")[..8].ToUpperInvariant()}",
            fullTemplate.Description);

        // Map status cũ → mới để khi clone transition còn liên kết được.
        var statusIdMap = new Dictionary<Guid, Guid>();
        foreach (var s in fullTemplate.Statuses.OrderBy(x => x.Order))
        {
            var newStatus = clone.AddStatus(s.Name, s.Key, s.Category, s.Color, s.Order);
            statusIdMap[s.Id] = newStatus.Id;
        }

        clone.SetInitialStatus(statusIdMap[fullTemplate.InitialStatusId]);

        foreach (var t in fullTemplate.Transitions)
        {
            var fromId = t.FromStatusId.HasValue ? statusIdMap[t.FromStatusId.Value] : (Guid?)null;
            var toId = statusIdMap[t.ToStatusId];
            clone.AddTransition(fromId, toId, t.Name, t.ScreenId, t.IsAutomatic);
        }

        await _workflowRepo.AddAsync(clone, ct);

        // Scheme: default = clone, không map per-issue-type (mọi issue type dùng default).
        var scheme = new WorkflowScheme(projectId, $"Default scheme for {projectId.ToString("N")[..8]}", clone.Id);
        await _schemeRepo.AddAsync(scheme, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Provisioned workflow scheme for project {ProjectId} (workflow={WorkflowId})",
            projectId, clone.Id);

        return Result.Success();
    }
}
