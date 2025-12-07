namespace LedgerCore.Core.ViewModels.Inventory;

public class InventoryAdjustmentCreateDto
{
    public string? Number { get; set; }  // اگر خالی باشد، بعداً می‌توانی با NumberSeries پرش کنی

    public DateTime Date { get; set; }

    public int WarehouseId { get; set; }

    public int? BranchId { get; set; }

    public string? Description { get; set; }

    public List<InventoryAdjustmentLineDto> Lines { get; set; } = new();
}