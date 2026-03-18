using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Assets;

public class DepreciationMethod: AuditableEntity
{
    public string Code { get; set; } = default!;   // مثلاً STRAIGHT_LINE
    public string Name { get; set; } = default!;   // خط مستقیم
    public string? Description { get; set; }

    /// <summary>
    /// آیا روش فعال است و می‌توان برای دارایی‌ها استفاده کرد؟
    /// </summary>
    public bool IsActive { get; set; } = true;
}