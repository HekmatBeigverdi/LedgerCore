namespace LedgerCore.Core.ViewModels.Reports;

public class FiscalStatusRowDto
{
    public int FiscalYearId { get; set; }
    public string FiscalYearName { get; set; } = default!;
    public DateTime FiscalYearStartDate { get; set; }
    public DateTime FiscalYearEndDate { get; set; }
    public bool FiscalYearIsClosed { get; set; }
    public DateTime? FiscalYearClosedAt { get; set; }

    public int FiscalPeriodId { get; set; }
    public int PeriodNumber { get; set; }
    public string FiscalPeriodName { get; set; } = default!;
    public DateTime FiscalPeriodStartDate { get; set; }
    public DateTime FiscalPeriodEndDate { get; set; }
    public bool FiscalPeriodIsClosed { get; set; }
    public DateTime? FiscalPeriodClosedAt { get; set; }
}