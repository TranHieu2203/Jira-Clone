namespace BB.Common.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
}

public static class CacheKeys
{
    public static string Workflow(Guid workflowId) => $"workflow:{workflowId}";
    public static string ProjectWorkflows(Guid projectId) => $"project:{projectId}:workflows";
    public static string CustomField(Guid fieldId) => $"cf:{fieldId}";
    public static string ProjectFieldContexts(Guid projectId) => $"project:{projectId}:cf-contexts";
    public static string ScreenScheme(Guid projectId, Guid issueTypeId) => $"project:{projectId}:itss:{issueTypeId}";
}
