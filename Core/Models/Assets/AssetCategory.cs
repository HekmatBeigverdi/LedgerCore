using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Assets;

public class AssetCategory: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>
    /// تعداد ماه عمر مفید پیش‌فرض (مثلاً 60 ماه)
    /// </summary>
    public int DefaultUsefulLifeMonths { get; set; }

    /// <summary>
    /// درصد ارزش اسقاط/باقیمانده (مثلاً 10 یعنی 10٪)
    /// </summary>
    public decimal DefaultResidualPercent { get; set; }
}