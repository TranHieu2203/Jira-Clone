namespace Sprint.Application;

public sealed record SprintDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int Status,
    IReadOnlyList<Guid> OrderedIssueIds);

public sealed record CreateSprintRequest(
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Goal);

public sealed record UpdateSprintRequest(
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Goal);

public sealed record ReorderSprintIssuesRequest(IReadOnlyList<Guid> IssueIds);

public sealed record BurndownDayDto(string Date, decimal IdealRemaining, decimal ActualRemaining);

public sealed record SprintBurndownDto(
    Guid SprintId,
    decimal TotalPoints,
    IReadOnlyList<BurndownDayDto> Days);

/// <summary>Một entry trong velocity chart — committed vs completed story points của 1 sprint.</summary>
public sealed record SprintVelocityEntryDto(
    Guid SprintId,
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal Committed,
    decimal Completed);

/// <summary>
/// Velocity report cho 1 project — list các sprint đã completed gần đây nhất theo EndDate asc.
/// AverageCompleted = trung bình "completed" của các sprint đã có dữ liệu (committed > 0).
/// </summary>
public sealed record VelocityReportDto(
    Guid ProjectId,
    IReadOnlyList<SprintVelocityEntryDto> Sprints,
    decimal AverageCompleted);
