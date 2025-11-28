using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Cheques;

public class RegisterChequeRequest
{
    public string ChequeNumber { get; set; } = default!;
    public string? Serial { get; set; }

    public bool IsIncoming { get; set; } = true;

    public int? PartyId { get; set; }
    public int? BankAccountId { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    public string? Description { get; set; }
}

public class ChangeChequeStatusRequest
{
    public ChequeStatus NewStatus { get; set; }
    public string? Comment { get; set; }
}