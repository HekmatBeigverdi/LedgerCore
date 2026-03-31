using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Payroll;

public class PayrollItemTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PayrollItemNature Nature { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsInsurable { get; set; }
    public bool IsActive { get; set; }
}