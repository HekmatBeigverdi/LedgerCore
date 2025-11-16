using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Payroll;

public class PayrollLine: BaseEntity
{
    public int PayrollDocumentId { get; set; }
    public PayrollDocument? PayrollDocument { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal GrossAmount { get; set; }       // مجموع حقوق و مزایا
    public decimal Deductions { get; set; }        // مجموع کسورات (مالیات، بیمه، ...)
    public decimal NetAmount { get; set; }         // خالص پرداختی

    public string? Description { get; set; }
}