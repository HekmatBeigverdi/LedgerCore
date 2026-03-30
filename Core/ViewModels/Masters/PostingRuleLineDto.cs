using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Masters;

public class PostingRuleLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }

    public PostingLineSide Side { get; set; }
    public PostingAmountSource AmountSource { get; set; }

    public decimal? FixedAmount { get; set; }

    public int AccountId { get; set; }

    public bool UsePartyFromDocument { get; set; }
    public bool IsActive { get; set; }

    public string? DescriptionTemplate { get; set; }
}