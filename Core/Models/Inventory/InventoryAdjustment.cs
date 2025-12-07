using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class InventoryAdjustment: AuditableEntity
{
    public string Number { get; set; } = default!;         // شماره سند تعدیل
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    // جمع مقدار ریالی تعدیل (برای لینک با حسابداری)
    public decimal? TotalDifferenceValue { get; set; }
    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
}