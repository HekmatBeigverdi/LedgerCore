namespace LedgerCore.Core.Models.Accounting;

public class TrialBalanceRow
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;

    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }

    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }

    public decimal ClosingDebit => OpeningDebit + PeriodDebit - OpeningCredit - PeriodCredit > 0
        ? OpeningDebit + PeriodDebit - OpeningCredit - PeriodCredit
        : 0;

    public decimal ClosingCredit => OpeningDebit + PeriodDebit - OpeningCredit - PeriodCredit < 0
        ? Math.Abs(OpeningDebit + PeriodDebit - OpeningCredit - PeriodCredit)
        : 0;
}