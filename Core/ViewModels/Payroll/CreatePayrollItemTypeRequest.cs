using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Payroll;

public class CreatePayrollItemTypeRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PayrollItemNature Nature { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsInsurable { get; set; }
    public bool IsActive { get; set; } = true;
}