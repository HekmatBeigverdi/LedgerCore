using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Reports;

public class BalanceSheetLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;

    public AccountType AccountType { get; set; }

    /// <summary>
    /// مانده نهایی حساب به صورت جداگانه در ستون بدهکار/بستانکار
    /// </summary>
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}