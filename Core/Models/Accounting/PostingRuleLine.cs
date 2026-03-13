using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Models.Accounting;

/// <summary>
/// خط Rule ثبت حسابداری.
/// هر خط مشخص می‌کند مبلغ از کجا بیاید، در کدام سمت ثبت شود و به چه حسابی بخورد.
/// </summary>
public class PostingRuleLine : AuditableEntity
{
    public int PostingRuleId { get; set; }
    public PostingRule PostingRule { get; set; } = default!;

    public int LineNumber { get; set; }

    public PostingLineSide Side { get; set; }
    public PostingAmountSource AmountSource { get; set; }

    public decimal? FixedAmount { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public bool UsePartyFromDocument { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public string? DescriptionTemplate { get; set; }
}