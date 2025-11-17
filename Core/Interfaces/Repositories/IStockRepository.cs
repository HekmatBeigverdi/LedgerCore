using LedgerCore.Core.Models.Inventory;

namespace LedgerCore.Core.Interfaces.Repositories;

public interface IStockRepository
{
    Task<StockItem?> GetStockItemAsync(int warehouseId, int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMove>> GetStockMovesAsync(int productId, int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task AddStockMoveAsync(StockMove move, CancellationToken cancellationToken = default);
    void UpdateStockItem(StockItem item);
    Task AddStockItemAsync(StockItem item, CancellationToken cancellationToken = default);
}