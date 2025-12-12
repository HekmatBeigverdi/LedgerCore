namespace LedgerCore.Core.Models.Security;

public static class PermissionCodes
{
    // Dashboard
    public const string Dashboard_View = "Dashboard.View";
    public const string Dashboard_BranchSummary_View = "Dashboard.BranchSummary.View";

    // Inventory
    public const string Inventory_StockItem_View = "Inventory.StockItem.View";
    public const string Inventory_StockCard_View = "Inventory.StockCard.View";
    public const string Inventory_Adjustment_View = "Inventory.Adjustment.View";
    public const string Inventory_Adjustment_Create = "Inventory.Adjustment.Create";
    public const string Inventory_Adjustment_Process = "Inventory.Adjustment.Process";
    public const string Inventory_Adjustment_Post = "Inventory.Adjustment.Post";

    // Reports
    public const string Reports_Sales_View = "Reports.Sales.View";
    public const string Reports_Stock_View = "Reports.Stock.View";
    public const string Reports_TrialBalance_View = "Reports.TrialBalance.View";
    public const string Reports_FiscalStatus_View = "Reports.FiscalStatus.View"; // جدید

    // Approval
    public const string Approval_Request_View = "Approval.Request.View";
    public const string Approval_Request_Create = "Approval.Request.Create";
    public const string Approval_Request_Approve = "Approval.Request.Approve";
    public const string Approval_Request_Reject = "Approval.Request.Reject";

    // ============ جدید: Accounting ============

    // Journals
    public const string Accounting_Journal_View = "Accounting.Journal.View";
    public const string Accounting_Journal_Create = "Accounting.Journal.Create";
    public const string Accounting_Journal_Edit = "Accounting.Journal.Edit";
    public const string Accounting_Journal_Delete = "Accounting.Journal.Delete";
    public const string Accounting_Journal_Post = "Accounting.Journal.Post";

    // FiscalYear
    public const string Accounting_FiscalYear_View = "Accounting.FiscalYear.View";
    public const string Accounting_FiscalYear_Manage = "Accounting.FiscalYear.Manage";

    // FiscalPeriod
    public const string Accounting_FiscalPeriod_View = "Accounting.FiscalPeriod.View";
    public const string Accounting_FiscalPeriod_Manage = "Accounting.FiscalPeriod.Manage";
    public const string Accounting_FiscalPeriod_Close = "Accounting.FiscalPeriod.Close";
    public const string Accounting_FiscalPeriod_Open = "Accounting.FiscalPeriod.Open";
}
