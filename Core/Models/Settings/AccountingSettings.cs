using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Settings;

public class AccountingSettings: AuditableEntity
{
    // Accounts Receivable / Payable
    public int ReceivableAccountId { get; set; }
    public int PayableAccountId { get; set; }

    // Revenue & Purchase
    public int SalesRevenueAccountId { get; set; }
    public int SalesReturnAccountId { get; set; }
    public int PurchaseAccountId { get; set; }
    public int PurchaseReturnAccountId { get; set; }

    // Tax
    public int SalesVatAccountId { get; set; }
    public int PurchaseVatAccountId { get; set; }

    // Cash & Bank
    public int CashAccountId { get; set; }
    public int BankAccountId { get; set; }

    // Inventory & COGS
    public int InventoryAccountId { get; set; }
    public int CogsAccountId { get; set; }

    // Payroll
    public int PayrollExpenseAccountId { get; set; }
    public int PayrollPayableAccountId { get; set; }

    // Fixed Assets
    public int FixedAssetAccountId { get; set; }
    public int AccumulatedDepreciationAccountId { get; set; }
    public int DepreciationExpenseAccountId { get; set; }
}