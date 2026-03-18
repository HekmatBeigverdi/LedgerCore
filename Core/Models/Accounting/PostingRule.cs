using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Accounting;

/// <summary>
/// هدر Rule ثبت حسابداری برای یک نوع سند.
/// خطوط ثبت در PostingRuleLine نگهداری می‌شوند.
/// </summary>
public class PostingRule : AuditableEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DocumentType { get; set; } = default!;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public bool IsActive { get; set; } = true;
    public bool AutoPost { get; set; } = true;
    public int Priority { get; set; } = 0;

    public ICollection<PostingRuleLine> Lines { get; set; } = new List<PostingRuleLine>();
}