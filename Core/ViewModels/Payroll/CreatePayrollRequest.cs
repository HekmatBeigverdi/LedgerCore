using System.ComponentModel.DataAnnotations;

namespace LedgerCore.Core.ViewModels.Payroll;

public class CreatePayrollRequest
{
    [Required]
    public int PayrollPeriodId { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? BranchId { get; set; }

    [MinLength(1, ErrorMessage = "حداقل یک ردیف حقوق لازم است.")]
    public List<CreatePayrollLineRequest> Lines { get; set; } = new();
}