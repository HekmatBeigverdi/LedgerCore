using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Models.Common;
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

    public async Task<PagedResult<StockMove>> GetStockMovesAsync(int productId, int? warehouseId = null,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StockMove> query = context.StockMoves
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Where(x => x.ProductId == productId)
            .AsNoTracking();

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        query = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);

        return await QueryHelpers.ApplyPagingAsync(query, paging, cancellationToken);
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