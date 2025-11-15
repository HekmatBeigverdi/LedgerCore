using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Accounting;

public class Account: AuditableEntity
{
    public string Code { get; set; } = default!;           // کد حساب (مثلاً 10101)
    public string Name { get; set; } = default!;           // نام حساب (مثلاً موجودی نقد)
    
    public AccountType Type { get; set; }                  // نوع حساب (دارایی، بدهی، ...)
    public BalanceSide NormalSide { get; set; }            // سمت مانده طبیعی (بدهکار/بستانکار)

    public int Level { get; set; }                         // سطح در کدینگ (۱،۲،۳...)
    public bool IsPosting { get; set; } = true;            // آیا حساب ثبت‌پذیر است یا فقط سرگروه؟

    public int? ParentAccountId { get; set; }              // حساب والد
    public Account? ParentAccount { get; set; }

    public bool IsActive { get; set; } = true;
}