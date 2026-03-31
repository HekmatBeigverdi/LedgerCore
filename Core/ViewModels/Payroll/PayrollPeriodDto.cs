namespace LedgerCore.Core.ViewModels.Payroll;

public class PayrollPeriodDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? FiscalPeriodId { get; set; }
    public string? FiscalPeriodName { get; set; }
}