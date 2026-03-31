namespace LedgerCore.Core.ViewModels.Payroll;

public class UpdatePayrollPeriodRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public int? FiscalPeriodId { get; set; }
}