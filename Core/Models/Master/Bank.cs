using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class Bank: AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }    // کد بانک اگر نیاز داشته باشیم
}