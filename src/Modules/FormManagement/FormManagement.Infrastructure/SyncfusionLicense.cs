using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;

namespace FormManagement.Infrastructure;

/// <summary>
/// Đăng ký Syncfusion Community License cho BE (cùng key với FE).
/// Key đọc từ <c>Syncfusion:LicenseKey</c> trong appsettings hoặc env var <c>SYNCFUSION__LICENSEKEY</c>.
/// Nếu trống, DocIO vẫn chạy ở chế độ trial — output có watermark (đủ để dev/test).
/// </summary>
public static class SyncfusionLicense
{
    public const string ConfigKey = "Syncfusion:LicenseKey";

    public static void Register(IConfiguration cfg, ILogger? logger = null)
    {
        var key = cfg.GetValue<string>(ConfigKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            logger?.LogWarning(
                "[Syncfusion] License key chưa cấu hình ({Path}) — DocIO mail-merge sẽ output có watermark trial.",
                ConfigKey);
            return;
        }
        SyncfusionLicenseProvider.RegisterLicense(key);
        logger?.LogInformation("[Syncfusion] License đã register cho BE DocIO.");
    }
}
