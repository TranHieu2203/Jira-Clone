using BB.Persistence;
using Issue.Domain;

namespace Issue.Application.Repositories;

public interface ISavedFilterRepository : IRepository<SavedFilter>
{
    /// <summary>List filter user có thể nhìn thấy: filter của chính user + filter shared.</summary>
    Task<IReadOnlyList<SavedFilter>> ListVisibleToUserAsync(Guid userId, CancellationToken ct = default);
}
