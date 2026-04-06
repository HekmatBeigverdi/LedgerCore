using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Inventory;

public class WarehouseTransferDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public int BranchId { get; set; }

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; }

    public List<WarehouseTransferLineDto> Lines { get; set; } = new();
}