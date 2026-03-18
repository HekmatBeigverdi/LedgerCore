using LedgerCore.Core.Models.Inventory;

namespace LedgerCore.Core.Interfaces.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<StockMove>> GetStockCardAsync(
        int productId,
        int? warehouseId,
        CancellationToken cancellationToken = default);

    Task<StockItem?> GetStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default);

    Task ProcessInventoryAdjustmentAsync(
        InventoryAdjustment adjustment,
        CancellationToken cancellationToken = default);
}