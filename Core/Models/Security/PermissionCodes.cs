namespace LedgerCore.Core.Models.Security;

public static class PermissionCodes
{
    // ========== Dashboard ==========
    public const string Dashboard_View = "Dashboard.View";
    public const string Dashboard_BranchSummary_View = "Dashboard.BranchSummary.View";

    // ========== Sales ==========
    public const string Sales_Invoice_View = "Sales.Invoice.View";
    public const string Sales_Invoice_Create = "Sales.Invoice.Create";
    public const string Sales_Invoice_Edit = "Sales.Invoice.Edit";
    public const string Sales_Invoice_Post = "Sales.Invoice.Post";
    public const string Sales_Return_View = "Sales.Return.View";
    public const string Sales_Return_Create = "Sales.Return.Create";
    public const string Sales_Return_Edit = "Sales.Return.Edit";
    public const string Sales_Return_Post = "Sales.Return.Post";

    // ========== Inventory ==========
    public const string Inventory_StockItem_View = "Inventory.StockItem.View";
    public const string Inventory_StockCard_View = "Inventory.StockCard.View";
    public const string Inventory_Adjustment_View = "Inventory.Adjustment.View";
    public const string Inventory_Adjustment_Create = "Inventory.Adjustment.Create";
    public const string Inventory_Adjustment_Process = "Inventory.Adjustment.Process";
    public const string Inventory_Adjustment_Post = "Inventory.Adjustment.Post";
    public const string Inventory_Transfer_View = "Inventory.Transfer.View";
    public const string Inventory_Transfer_Create = "Inventory.Transfer.Create";
    public const string Inventory_Transfer_Edit = "Inventory.Transfer.Edit";
    public const string Inventory_Transfer_Post = "Inventory.Transfer.Post";

    // ========== Reports ==========
    public const string Reports_Sales_View = "Reports.Sales.View";
    public const string Reports_Stock_View = "Reports.Stock.View";
    public const string Reports_TrialBalance_View = "Reports.TrialBalance.View";
    public const string Reports_FiscalStatus_View = "Reports.FiscalStatus.View"; // جدید
    public const string Reports_SubLedger_View = "Reports.SubLedger.View";
    public const string Reports_Aging_View = "Reports.Aging.View";
    public const string Reports_Inventory_StockCard_View = "Reports.Inventory.StockCard.View";
    public const string Reports_Sales_ByParty_View = "Reports.Sales.ByParty.View";
    public const string Reports_Purchases_ByParty_View = "Reports.Purchases.ByParty.View";
    public const string Reports_Payroll_Summary_View = "Reports.Payroll.Summary.View";
    public const string Reports_Payroll_Details_View = "Reports.Payroll.Details.View";
    

    // ========== Approval / Workflow ==========
    public const string Approval_Request_View = "Approval.Request.View";
    public const string Approval_Request_Create = "Approval.Request.Create";
    public const string Approval_Request_Approve = "Approval.Request.Approve";
    public const string Approval_Request_Reject = "Approval.Request.Reject";

    // ========== Accounting (جدید) ==========

    // Journals
    public const string Accounting_Journal_View = "Accounting.Journal.View";
    public const string Accounting_Journal_Create = "Accounting.Journal.Create";
    public const string Accounting_Journal_Edit = "Accounting.Journal.Edit";
    public const string Accounting_Journal_Delete = "Accounting.Journal.Delete";
    public const string Accounting_Journal_Post = "Accounting.Journal.Post";

    // FiscalYear
    public const string Accounting_FiscalYear_View = "Accounting.FiscalYear.View";
    public const string Accounting_FiscalYear_Manage = "Accounting.FiscalYear.Manage";
    
    // Fiscal (Close and Open year)
    public const string Accounting_Fiscal_Close = "Accounting.Fiscal.Close";
    public const string Accounting_FiscalYear_Open  = "Accounting.FiscalYear.Open";

    // FiscalPeriod
    public const string Accounting_FiscalPeriod_View = "Accounting.FiscalPeriod.View";
    public const string Accounting_FiscalPeriod_Manage = "Accounting.FiscalPeriod.Manage";
    public const string Accounting_FiscalPeriod_Close = "Accounting.FiscalPeriod.Close";
    public const string Accounting_FiscalPeriod_Open = "Accounting.FiscalPeriod.Open";
    
    // ========== Payroll ==========
    public const string Payroll_View = "Payroll.View";
    public const string Payroll_Manage = "Payroll.Manage";
    public const string Payroll_Post = "Payroll.Post";

    // ========== FixedAssets ==========
    public const string Assets_View = "Assets.View";
    public const string Assets_Manage = "Assets.Manage";
    public const string Assets_Depreciation_Post = "Assets.Depreciation.Post";
    
    // ========== Purchase ==========
    public const string Purchase_Invoice_View = "Purchase.Invoice.View";
    public const string Purchase_Invoice_Create = "Purchase.Invoice.Create";
    public const string Purchase_Invoice_Edit = "Purchase.Invoice.Edit";
    public const string Purchase_Invoice_Post = "Purchase.Invoice.Post";
    public const string Purchase_Return_View = "Purchase.Return.View";
    public const string Purchase_Return_Create = "Purchase.Return.Create";
    public const string Purchase_Return_Edit = "Purchase.Return.Edit";
    public const string Purchase_Return_Post = "Purchase.Return.Post";

    // ========== Receipts ==========
    public const string Receipt_View = "Receipt.View";
    public const string Receipt_Create = "Receipt.Create";
    public const string Receipt_Edit = "Receipt.Edit";
    public const string Receipt_Post = "Receipt.Post";
    public const string Receipt_Reverse = "Receipt.Reverse";

    // ========== Payments ==========
    public const string Payment_View = "Payment.View";
    public const string Payment_Create = "Payment.Create";
    public const string Payment_Edit = "Payment.Edit";
    public const string Payment_Post = "Payment.Post";
    public const string Payment_Reverse = "Payment.Reverse";

    // ========== CashTransfer ==========
    public const string CashTransfer_View = "CashTransfer.View";
    public const string CashTransfer_Create = "CashTransfer.Create";
    public const string CashTransfer_Post = "CashTransfer.Post";

    // ========== Cheques ==========
    public const string Cheque_View = "Cheque.View";
    public const string Cheque_Create = "Cheque.Create";
    public const string Cheque_Status_Change = "Cheque.Status.Change";

    // ========== Security ==========
    public const string Security_Permissions_View = "Security.Permissions.View";
    public const string Security_Roles_View = "Security.Roles.View";
    public const string Security_Logs_View = "Security.Logs.View";
    
    // ========== Masters ==========
    public const string Master_Accounts_View = "Master.Accounts.View";
    public const string Master_Accounts_Manage = "Master.Accounts.Manage";

    public const string Master_Parties_View = "Master.Parties.View";
    public const string Master_Parties_Manage = "Master.Parties.Manage";

    public const string Master_Products_View = "Master.Products.View";
    public const string Master_Products_Manage = "Master.Products.Manage";

    public const string Master_Warehouses_View = "Master.Warehouses.View";
    public const string Master_Warehouses_Manage = "Master.Warehouses.Manage";

    public const string Master_Branches_View = "Master.Branches.View";
    public const string Master_Branches_Manage = "Master.Branches.Manage";

    public const string Master_TaxRates_View = "Master.TaxRates.View";
    public const string Master_TaxRates_Manage = "Master.TaxRates.Manage";

    public const string Master_CostCenters_View = "Master.CostCenters.View";
    public const string Master_CostCenters_Manage = "Master.CostCenters.Manage";

    public const string Master_Projects_View = "Master.Projects.View";
    public const string Master_Projects_Manage = "Master.Projects.Manage";

    public const string Master_AccountingSettings_View = "Master.AccountingSettings.View";
    public const string Master_AccountingSettings_Manage = "Master.AccountingSettings.Manage";

    public const string Master_NumberSeries_View = "Master.NumberSeries.View";
    public const string Master_NumberSeries_Manage = "Master.NumberSeries.Manage";

    public const string Master_PostingRules_View = "Master.PostingRules.View";
    public const string Master_PostingRules_Manage = "Master.PostingRules.Manage";
}
