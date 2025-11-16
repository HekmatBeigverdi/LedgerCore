using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Payroll;

public class PayrollDocument: AuditableEntity
{
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public decimal TotalGross { get; set; }       // جمع حقوق و مزایا
    public decimal TotalDeductions { get; set; }  // جمع کسورات
    public decimal TotalNet { get; set; }         // جمع خالص پرداختی

    public int? JournalVoucherId { get; set; }    // سند حسابداری
    public JournalVoucher? JournalVoucher { get; set; }

    public ICollection<PayrollLine> Lines { get; set; } = new List<PayrollLine>();
}