using FormManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormManagement.Infrastructure.Seed;

/// <summary>
/// Seed metadata mặc định theo convention biểu mẫu bảo hiểm VN (form.md §3).
/// Idempotent — bỏ qua field đã tồn tại (kiểm tra theo Value); không ghi đè sửa đổi của admin.
/// </summary>
public static class FormManagementSeeder
{
    /// <summary>Danh sách metadata default — value + label + type. Group tự suy ra từ prefix.</summary>
    private static readonly IReadOnlyList<(string Value, string Label, MetadataType Type, string? Description)> DefaultMetadata = new[]
    {
        // B* — Thông tin hợp đồng
        ("BSO_HD",      "Số hợp đồng",                MetadataType.Text,     (string?)null),
        ("BNGAY_CAP",   "Ngày cấp hợp đồng",          MetadataType.Date,     (string?)null),
        ("BTHANG_HD",   "Tháng hợp đồng",             MetadataType.Date,     (string?)null),
        ("BNAM_HD",     "Năm hợp đồng",               MetadataType.Date,     (string?)null),
        ("BLOAI_HD",    "Loại hợp đồng",              MetadataType.Text,     (string?)null),

        // C* — Bên A (khách hàng)
        ("CTEN",        "Tên khách hàng",             MetadataType.Text,     "Tên đầy đủ của Bên A"),
        ("CDAI_DIEN",   "Người đại diện",             MetadataType.Text,     (string?)null),
        ("CCHUC_VU",    "Chức vụ người đại diện",     MetadataType.Text,     (string?)null),
        ("CMST",        "Mã số thuế",                 MetadataType.Text,     (string?)null),
        ("CDIA_CHI",    "Địa chỉ",                    MetadataType.Textarea, (string?)null),
        ("CSDT",        "Số điện thoại",              MetadataType.Text,     (string?)null),
        ("CEMAIL",      "Email liên hệ",              MetadataType.Text,     (string?)null),

        // D* — Số tiền bảo hiểm
        ("DSO_TIEN",    "Số tiền bảo hiểm",           MetadataType.Currency, "Số tiền bảo hiểm (VND)"),
        ("DTEXT",       "Số tiền bằng chữ",           MetadataType.Textarea, (string?)null),

        // F* — Điều khoản
        ("FDKBS",       "Điều khoản bổ sung",         MetadataType.Textarea, (string?)null),
        ("FGIOI_HAN",   "Giới hạn trách nhiệm",       MetadataType.Currency, (string?)null),

        // G* — Mức khấu trừ
        ("GMKT_A",      "Mức khấu trừ Bên A",         MetadataType.Currency, (string?)null),
        ("GLHNV_A",     "Loại hình nghiệp vụ Bên A",  MetadataType.Text,     (string?)null),

        // I* — Phí bảo hiểm (currency format vi-VN: #,##0)
        ("ITEN_DT",     "Tên đối tượng bảo hiểm",     MetadataType.Text,     (string?)null),
        ("IPHI_GOC",    "Phí bảo hiểm gốc",           MetadataType.Currency, (string?)null),
        ("IVAT",        "Thuế VAT",                   MetadataType.Currency, (string?)null),
        ("ITONG_PHI",   "Tổng phí bảo hiểm",          MetadataType.Currency, (string?)null),
        ("ITONG_VAT",   "Tổng VAT",                   MetadataType.Currency, (string?)null),

        // J* — Thanh toán
        ("JKY_TT",      "Kỳ thanh toán",              MetadataType.Text,     (string?)null),
        ("JNGAY",       "Ngày thanh toán",            MetadataType.Date,     (string?)null),
        ("JTIEN_TEXT",  "Số tiền thanh toán (chữ)",   MetadataType.Textarea, (string?)null),

        // K* — Người ký
        ("KTEN_NG_H",   "Tên người ký",               MetadataType.Text,     (string?)null),
        ("KCHUC_VU",    "Chức vụ người ký",           MetadataType.Text,     (string?)null),

        // L* — Nội dung
        ("LNOI_DUNG",   "Nội dung hợp đồng",          MetadataType.Textarea, (string?)null),
        ("LDIEU_KIEN",  "Điều kiện đi kèm",           MetadataType.Textarea, (string?)null),

        // M* — Khác
        ("MGIA_TRI",    "Giá trị khác",               MetadataType.Currency, (string?)null),
        ("MGHI_CHU",    "Ghi chú",                    MetadataType.Textarea, (string?)null)
    };

    public static async Task SeedDefaultsAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using IServiceScope scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<FormManagementDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FormManagementSeeder");

        // Tải set value đã tồn tại để skip nhanh (tránh N round-trip nếu seed nhiều field).
        var existing = await ctx.Metadata.AsNoTracking()
            .Select(m => m.Value)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        int added = 0;
        foreach (var (value, label, type, description) in DefaultMetadata)
        {
            if (existingSet.Contains(value)) continue;
            ctx.Metadata.Add(new MetadataDef(value, label, type, description));
            added++;
        }

        if (added > 0)
        {
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} default metadata fields (form_mgmt)", added);
        }
        else
        {
            logger.LogDebug("FormManagement metadata seed skipped — all default fields already exist");
        }
    }
}
