using BB.Common;

namespace Notification.Application;

public interface IEmailPreferenceService
{
    /// <summary>Lấy preference của current user — auto-tạo default (tất cả false) nếu chưa có.</summary>
    Task<Result<EmailUserPreferenceDto>> GetMineAsync(CancellationToken ct = default);

    /// <summary>Update preference của current user (upsert).</summary>
    Task<Result<EmailUserPreferenceDto>> UpdateMineAsync(UpdateEmailPreferenceRequest request, CancellationToken ct = default);
}
