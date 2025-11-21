using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Persistence.Repository;

public class StockRepository(LedgerCoreDbContext context) : IStockRepository
{
    public Task<StockItem?> GetStockItemAsync(int warehouseId, int productId, CancellationToken cancellationToken = default)
    {
        return context.StockItems
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId, cancellationToken);
    }

    public Task<IReadOnlyList<StockMove>> GetStockMovesAsync(
        int productId,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.StockMoves.AsQueryable()
            .Where(x => x.ProductId == productId);

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        return query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ContinueWith<IReadOnlyList<StockMove>>(t => t.Result, cancellationToken);
    }

    public async Task AddStockMoveAsync(StockMove move, CancellationToken cancellationToken = default)
    {
        await context.StockMoves.AddAsync(move, cancellationToken);
    }

    public void UpdateStockItem(StockItem item)
    {
        context.StockItems.Update(item);
    }

    public async Task AddStockItemAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        await context.StockItems.AddAsync(item, cancellationToken);
    }
}