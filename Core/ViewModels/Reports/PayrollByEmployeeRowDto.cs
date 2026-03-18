namespace LedgerCore.Core.ViewModels.Reports;

public class PayrollByEmployeeRowDto
{
    public int EmployeeId { get; set; }
    public string PersonnelCode { get; set; } = default!;
    public string FullName { get; set; } = default!;

    public int? BranchId { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    public int? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }

    public decimal TotalGross { get; set; }        // جمع حقوق و مزایا
    public decimal TotalDeductions { get; set; }   // جمع کسورات
    public decimal TotalNet { get; set; }          // جمع خالص پرداختی

    public int PayrollDocumentCount { get; set; }  // تعداد اسناد حقوقی که این کارمند در آنها حضور داشته
}