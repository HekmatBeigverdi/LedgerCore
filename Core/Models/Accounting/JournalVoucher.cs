using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Accounting;

public class JournalVoucher: AuditableEntity
{
    public string Number { get; set; } = default!;        // شماره سند
    public DateTime Date { get; set; } = DateTime.UtcNow; // تاریخ سند
    public string? Description { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? FiscalPeriodId { get; set; }
    public FiscalPeriod? FiscalPeriod { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}