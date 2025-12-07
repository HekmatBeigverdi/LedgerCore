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

    public async Task<PagedResult<StockMove>> GetStockMovesAsync(
        int productId,
        int? warehouseId = null,
        PagingParams? paging = null,
        CancellationToken cancellationToken = default)
    {
        // کوئری پایه: همه حرکات یک کالا
        var query = context.StockMoves
            .AsNoTracking()
            .Where(m => m.ProductId == productId);

        if (warehouseId.HasValue)
        {
            query = query.Where(m => m.WarehouseId == warehouseId.Value);
        }

        // مرتب‌سازی کارتکس: بر اساس تاریخ و Id
        query = query
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id);

        var pageNumber = paging?.PageNumber is > 0 ? paging.PageNumber : 1;
        var pageSize   = paging?.PageSize   is > 0 ? paging.PageSize   : 50;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMove>(items, totalCount, pageNumber, pageSize);
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