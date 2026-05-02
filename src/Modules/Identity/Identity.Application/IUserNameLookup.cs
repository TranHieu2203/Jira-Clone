namespace Identity.Application;

/// <summary>Resolve username → user id (mention trong comment).</summary>
public interface IUserNameLookup
{
    Task<Guid?> FindActiveUserIdByUserNameAsync(string userName, CancellationToken ct = default);
}
