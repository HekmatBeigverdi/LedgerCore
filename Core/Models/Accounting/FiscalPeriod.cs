using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Accounting;

public class FiscalPeriod: AuditableEntity
{
    public int FiscalYearId { get; set; }
    public FiscalYear? FiscalYear { get; set; }

    public int PeriodNumber { get; set; }              // 1..12 مثلاً
    public string Name { get; set; } = default!;       // مثال: 1403-01

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
}