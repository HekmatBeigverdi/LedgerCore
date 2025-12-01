using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Payroll;

public class PayrollDocumentDto
{
    public int Id { get; set; }

    public string Number { get; set; } = default!;
    public DateTime Date { get; set; }

    public int PayrollPeriodId { get; set; }
    public string PayrollPeriodCode { get; set; } = default!;
    public string PayrollPeriodName { get; set; } = default!;

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    public PayrollStatus Status { get; set; }

    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }

    public int? JournalVoucherId { get; set; }

    public List<PayrollLineDto> Lines { get; set; } = new();
}