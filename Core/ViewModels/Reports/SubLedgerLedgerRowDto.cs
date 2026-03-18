namespace LedgerCore.Core.ViewModels.Reports;

public class SubLedgerLedgerRowDto
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = default!;
    public string PartyName { get; set; } = default!;

    public int AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;

    public DateTime Date { get; set; }
    public string JournalNumber { get; set; } = default!;
    public string? Description { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public decimal BalanceDebit { get; set; }
    public decimal BalanceCredit { get; set; }
}