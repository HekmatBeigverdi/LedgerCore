using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Cheques;

public class ChequeDto
{
    public int Id { get; set; }

    public string ChequeNumber { get; set; } = default!;
    public string? Serial { get; set; }
    
    public bool IsIncoming { get; set; }          // true: دریافتی، false: صادره

    public int? PartyId { get; set; }             // صادرکننده/ذی‌نفع
    public string? PartyName { get; set; }

    public int? BankAccountId { get; set; }
    public string? BankAccountTitle { get; set; }

    public decimal Amount { get; set; }

    public int? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal FxRate { get; set; }

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    public ChequeStatus Status { get; set; }
    public string? Description { get; set; }

    public List<ChequeHistoryDto> History { get; set; } = new();
}