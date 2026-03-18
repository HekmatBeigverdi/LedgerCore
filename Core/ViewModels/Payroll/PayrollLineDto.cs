namespace LedgerCore.Core.ViewModels.Payroll;

public class PayrollLineDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public string EmployeePersonnelCode { get; set; } = default!;
    public string EmployeeFullName { get; set; } = default!;

    public decimal GrossAmount { get; set; }      // جمع مزایا
    public decimal Deductions { get; set; }       // جمع کسورات
    public decimal NetAmount { get; set; }        // خالص

    public string? Description { get; set; }
}