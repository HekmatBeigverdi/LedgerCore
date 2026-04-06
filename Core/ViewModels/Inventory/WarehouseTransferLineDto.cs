namespace LedgerCore.Core.ViewModels.Inventory;

public class WarehouseTransferLineDto
{
    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Description { get; set; }
}