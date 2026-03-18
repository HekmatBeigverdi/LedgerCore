using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Payroll;

public class PayrollItemType: AuditableEntity
{
    public string Code { get; set; } = default!;       // BASIC, OVERTIME, TAX, INSURANCE, ...
    public string Name { get; set; } = default!;       // حقوق پایه، اضافه کار، مالیات، بیمه، ...

    public PayrollItemNature Nature { get; set; }      // مزایا یا کسورات

    public bool IsTaxable { get; set; }                // آیا در محاسبه مالیات دخیل است؟
    public bool IsInsurable { get; set; }              // آیا در محاسبه بیمه دخیل است؟

    public bool IsActive { get; set; } = true;
}