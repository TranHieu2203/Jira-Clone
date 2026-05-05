namespace Issue.Application;

public sealed record SavedFilterDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string Jql,
    string? Description,
    bool IsShared,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateSavedFilterRequest(
    string Name,
    string Jql,
    string? Description,
    bool IsShared);

public sealed record UpdateSavedFilterRequest(
    string Name,
    string Jql,
    string? Description,
    bool IsShared);
