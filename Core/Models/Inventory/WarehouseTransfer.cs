using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class WarehouseTransfer : AuditableEntity
{
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public int FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }

    public int ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public List<WarehouseTransferLine> Lines { get; set; } = new();
}