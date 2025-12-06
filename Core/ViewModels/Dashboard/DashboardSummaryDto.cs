namespace LedgerCore.Core.ViewModels.Dashboard;

public class DashboardSummaryDto
{
    public DateTime Today { get; set; }
    public DateTime MonthStart { get; set; }
    public DateTime MonthEnd { get; set; }

    public int? BranchId { get; set; }

    // ===== فروش =====
    public int TodaySalesInvoiceCount { get; set; }
    public decimal TodaySalesTotal { get; set; }
    public int ThisMonthSalesInvoiceCount { get; set; }
    public decimal ThisMonthSalesTotal { get; set; }

    // ===== خرید =====
    public int TodayPurchaseInvoiceCount { get; set; }
    public decimal TodayPurchaseTotal { get; set; }
    public int ThisMonthPurchaseInvoiceCount { get; set; }
    public decimal ThisMonthPurchaseTotal { get; set; }

    // ===== نقد / بانک (ورودی و خروجی پول) =====
    public decimal TodayReceiptsTotal { get; set; }
    public decimal TodayPaymentsTotal { get; set; }
    public decimal ThisMonthReceiptsTotal { get; set; }
    public decimal ThisMonthPaymentsTotal { get; set; }

    // ===== طرف حساب‌ها =====
    public int CustomerCount { get; set; }
    public int SupplierCount { get; set; }

    // ===== فاکتورهای فروش معوق =====
    public int OverdueSalesInvoiceCount { get; set; }
    public decimal OverdueSalesInvoiceAmount { get; set; }
}