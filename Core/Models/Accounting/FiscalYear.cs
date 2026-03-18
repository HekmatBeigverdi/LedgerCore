using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Accounting;

public class FiscalYear: AuditableEntity
{
    public string Name { get; set; } = default!;        // مثال: 1403، 2025
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
}