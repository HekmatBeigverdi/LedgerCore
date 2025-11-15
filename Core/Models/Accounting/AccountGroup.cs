using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Accounting;

public class AccountGroup: AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public AccountType Type { get; set; }                  // نوع کلی حساب‌های این گروه
    public int? ParentGroupId { get; set; }
    public AccountGroup? ParentGroup { get; set; }
}