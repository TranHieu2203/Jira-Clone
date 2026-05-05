using BB.Common;

namespace BB.Security;

/// <summary>
/// Helper kiểm tra quyền ở Application service. Trả <see cref="Result"/> để service
/// trả thẳng tới controller mà không cần boilerplate try/catch.
///
/// Dùng pattern:
/// <code>
/// var perm = await _permissions.RequireProjectAsync(_currentUser.UserId, projectId, PermissionKeys.IssueEdit, ct);
/// if (perm.IsFailure) return Result.Failure&lt;Dto&gt;(perm);
/// </code>
/// </summary>
public static class PermissionCheckerExtensions
{
    public const string MessageKeyDenied = "permission.denied";

    /// <summary>
    /// Kiểm tra quyền ở phạm vi project. Trả Forbidden nếu user không là thành viên project hoặc role không đủ.
    /// </summary>
    public static async Task<Result> RequireProjectAsync(
        this IPermissionChecker checker,
        Guid? userId,
        Guid projectId,
        string permission,
        CancellationToken ct = default)
    {
        if (userId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        if (!await checker.HasProjectPermissionAsync(userId.Value, projectId, permission, ct))
            return Result.Failure(ErrorType.Forbidden, MessageKeyDenied);

        return Result.Success();
    }

    /// <summary>
    /// Kiểm tra quyền ở phạm vi org/workspace.
    /// </summary>
    public static async Task<Result> RequireOrgAsync(
        this IPermissionChecker checker,
        Guid? userId,
        Guid orgId,
        string permission,
        CancellationToken ct = default)
    {
        if (userId is null)
            return Result.Failure(ErrorType.Unauthorized, "auth.required");

        if (!await checker.HasOrgPermissionAsync(userId.Value, orgId, permission, ct))
            return Result.Failure(ErrorType.Forbidden, MessageKeyDenied);

        return Result.Success();
    }
}
