namespace LedgerCore.Core.ViewModels.Reports;

public class PayrollSummaryRowDto
{
    public int? BranchId { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    public int? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }

    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }

    public int EmployeeCount { get; set; }         // تعداد پرسنل در این ترکیب شعبه/مرکز هزینه
    public int PayrollDocumentCount { get; set; }  // چند سند حقوقی اینجا دخیل بوده
}