namespace LedgerCore.Core.ViewModels.Dashboard;

public class BranchDashboardSummaryRowDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal SalesTotal { get; set; }
    public decimal PurchaseTotal { get; set; }
    public decimal ReceiptsTotal { get; set; }
    public decimal PaymentsTotal { get; set; }

    public int OverdueSalesInvoiceCount { get; set; }
    public decimal OverdueSalesInvoiceAmount { get; set; }
}