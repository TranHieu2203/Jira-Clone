namespace BB.Security;

/// <summary>
/// Cross-module contract: lookup email by userId without referencing Identity module directly.
/// </summary>
public interface IUserEmailLookup
{
    Task<string?> FindEmailByIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> FindEmailsByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}

