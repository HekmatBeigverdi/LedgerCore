using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Inventory;

public class WarehouseTransferLine : BaseEntity
{
    public int WarehouseTransferId { get; set; }
    public WarehouseTransfer? WarehouseTransfer { get; set; }

    public int LineNumber { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>
    /// بهای واحدی که هنگام Post از انبار مبدا گرفته می‌شود.
    /// </summary>
    public decimal? UnitCost { get; set; }

    public string? Description { get; set; }
}