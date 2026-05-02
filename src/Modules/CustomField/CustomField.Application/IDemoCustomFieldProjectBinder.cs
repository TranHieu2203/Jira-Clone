namespace CustomField.Application;

/// <summary>
/// Gắn context demo (không global) cho một project — idempotent.
/// </summary>
public interface IDemoCustomFieldProjectBinder
{
    Task EnsureContextsForProjectAsync(Guid projectId, CancellationToken ct = default);
}
