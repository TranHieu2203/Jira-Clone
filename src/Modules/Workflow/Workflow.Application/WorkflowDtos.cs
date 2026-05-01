namespace Workflow.Application;

public sealed record WorkflowDto(
    Guid Id,
    Guid? ProjectId,
    string Name,
    string Key,
    string? Description,
    bool IsTemplate,
    bool IsActive,
    Guid InitialStatusId,
    IReadOnlyList<WorkflowStatusDto> Statuses,
    IReadOnlyList<WorkflowTransitionDto> Transitions);

public sealed record WorkflowStatusDto(
    Guid Id,
    string Name,
    string Key,
    int Category,
    string? Color,
    int Order,
    bool IsFinal);

public sealed record WorkflowTransitionDto(
    Guid Id,
    Guid? FromStatusId,
    Guid ToStatusId,
    string Name,
    Guid? ScreenId,
    bool IsAutomatic,
    IReadOnlyList<TransitionStepDto> Rules,
    IReadOnlyList<TransitionStepDto> Validators,
    IReadOnlyList<TransitionStepDto> PostFunctions);

public sealed record TransitionStepDto(Guid Id, string TypeKey, string ConfigJson, int Order);

public sealed record CreateWorkflowRequest(
    Guid? ProjectId,
    string Name,
    string Key,
    string? Description,
    bool IsTemplate);

public sealed record UpdateWorkflowRequest(string Name, string? Description, bool IsActive);

public sealed record AddStatusRequest(string Name, string Key, int Category, string? Color, int? Order);
public sealed record AddTransitionRequest(Guid? FromStatusId, Guid ToStatusId, string Name, Guid? ScreenId, bool IsAutomatic);
public sealed record AddTransitionStepRequest(string TypeKey, string ConfigJson, int? Order);

public sealed record TransitionExecuteRequest(
    Guid IssueId,
    Guid ProjectId,
    Guid IssueTypeId,
    Guid CurrentStatusId,
    Guid TransitionId,
    Dictionary<string, System.Text.Json.JsonElement>? Inputs,
    string? Comment);

public sealed record AvailableTransitionDto(
    Guid Id,
    string Name,
    Guid ToStatusId,
    string ToStatusName,
    Guid? ScreenId);
