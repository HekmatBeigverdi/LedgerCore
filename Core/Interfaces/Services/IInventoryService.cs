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
    
    Task<WarehouseTransfer> CreateWarehouseTransferAsync(
        WarehouseTransfer transfer,
        IReadOnlyList<WarehouseTransferLine> lines,
        CancellationToken cancellationToken = default);

    Task<WarehouseTransfer?> GetWarehouseTransferAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<WarehouseTransfer> UpdateWarehouseTransferAsync(
        int id,
        WarehouseTransfer transfer,
        IReadOnlyList<WarehouseTransferLine> lines,
        CancellationToken cancellationToken = default);

    Task PostWarehouseTransferAsync(
        int id,
        CancellationToken cancellationToken = default);
}