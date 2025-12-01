namespace LedgerCore.Core.ViewModels.Payroll;

public class CreatePayrollLineRequest
{
    public int EmployeeId { get; set; }

    public decimal GrossAmount { get; set; }      // حقوق و مزایا
    public decimal Deductions { get; set; }       // کسورات
    public string? Description { get; set; }
}