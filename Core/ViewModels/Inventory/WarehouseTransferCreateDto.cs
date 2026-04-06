namespace LedgerCore.Core.ViewModels.Inventory;

public class WarehouseTransferCreateDto
{
    public string? Number { get; set; }

    public DateTime Date { get; set; }

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public int? BranchId { get; set; }

    public string? Description { get; set; }

    public List<WarehouseTransferLineDto> Lines { get; set; } = new();
}