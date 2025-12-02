using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Reports;

public class ProfitAndLossLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;
    public AccountType AccountType { get; set; }

    /// <summary>
    /// مبلغ مثبت بر اساس سمت طبیعی حساب
    /// برای درآمد = مانده بستانکار، برای هزینه = مانده بدهکار
    /// </summary>
    public decimal Amount { get; set; }
}