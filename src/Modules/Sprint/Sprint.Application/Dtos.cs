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
