namespace LedgerCore.Core.ViewModels.Reports;

public class SubLedgerBalanceRowDto
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = default!;
    public string PartyName { get; set; } = default!;
    public string PartyType { get; set; } = default!;

    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }

    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }

    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
}