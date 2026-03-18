using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Payroll;

public class PayrollPeriod: AuditableEntity
{
    public string Code { get; set; } = default!;        // مثال: 1403-01
    public string Name { get; set; } = default!;        // مثال: فروردین 1403

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }

    public int? FiscalPeriodId { get; set; }            // لینک اختیاری به دوره مالی
    public FiscalPeriod? FiscalPeriod { get; set; }
}