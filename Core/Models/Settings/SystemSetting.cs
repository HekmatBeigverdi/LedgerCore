using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Settings;

public class SystemSetting: AuditableEntity
{
    public string Key { get; set; } = default!;        // مثلا "CompanyName", "BaseCurrency"
    public string? Value { get; set; }                 // مقدار متنی
}