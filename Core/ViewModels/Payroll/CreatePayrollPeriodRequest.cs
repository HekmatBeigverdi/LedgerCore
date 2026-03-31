namespace LedgerCore.Core.ViewModels.Payroll;

public class CreatePayrollPeriodRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; } = false;
    public int? FiscalPeriodId { get; set; }
}