namespace LedgerCore.Core.Models.Master;

public class AccountingSettingsDto
{
    public int Id { get; set; }

    public int ReceivableAccountId { get; set; }
    public int PayableAccountId { get; set; }

    public int SalesRevenueAccountId { get; set; }
    public int SalesReturnAccountId { get; set; }
    public int PurchaseAccountId { get; set; }
    public int PurchaseReturnAccountId { get; set; }

    public int SalesVatAccountId { get; set; }
    public int PurchaseVatAccountId { get; set; }

    public int CashAccountId { get; set; }
    public int BankAccountId { get; set; }

    public int InventoryAccountId { get; set; }
    public int CogsAccountId { get; set; }

    public int PayrollExpenseAccountId { get; set; }
    public int PayrollPayableAccountId { get; set; }

    public int FixedAssetAccountId { get; set; }
    public int AccumulatedDepreciationAccountId { get; set; }
    public int DepreciationExpenseAccountId { get; set; }
}