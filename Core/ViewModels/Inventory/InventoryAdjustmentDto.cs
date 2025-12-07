using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Inventory;

public class InventoryAdjustmentDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int WarehouseId { get; set; }

    public int? BranchId { get; set; }

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }

    public decimal? TotalDifferenceValue { get; set; }

    public int? JournalVoucherId { get; set; }

    public List<InventoryAdjustmentLineDto> Lines { get; set; } = new();
}