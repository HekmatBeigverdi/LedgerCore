using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

/// <summary>
/// سرویس دامنه‌ی انبار: کارتکس، وضعیت موجودی کالا و پردازش سند تعدیل.
/// </summary>
public class InventoryService(
    LedgerCoreDbContext db,
    IStockRepository stockRepository,
    ICurrentBranchService currentBranch,
    INumberSeriesService numberSeries) : IInventoryService
{
    private readonly LedgerCoreDbContext _db =
        db ?? throw new ArgumentNullException(nameof(db));

    private readonly IStockRepository _stock =
        stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));

    /// <summary>
    /// کارتکس یک کالا در یک انبار (اختیاری).
    /// </summary>
    public async Task<IReadOnlyList<StockMove>> GetStockCardAsync(
        int productId,
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var query = _db.StockMoves
            .AsNoTracking()
            .Include(m => m.Warehouse)
            .Where(m => m.ProductId == productId && m.Warehouse!.BranchId == branchId);

        if (warehouseId.HasValue)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == warehouseId.Value && x.BranchId == branchId, cancellationToken);

            if (warehouse is null)
                return Array.Empty<StockMove>();

            query = query.Where(m => m.WarehouseId == warehouseId.Value);
        }

        return await query
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// وضعیت موجودی یک کالا در یک انبار (OnHand / Reserved / AverageCost).
    /// اگر رکوردی نباشد، null برمی‌گردد.
    /// </summary>
    public async Task<StockItem?> GetStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.BranchId == branchId, cancellationToken);

        if (warehouse is null)
            return null;

        return await _stock.GetStockItemAsync(warehouseId, productId, cancellationToken);
    }

    /// <summary>
    /// پردازش سند تعدیل موجودی:
    /// - پیدا کردن StockMoveهای مربوط به این Adjustment
    /// - به‌روزرسانی StockItem (OnHand و AverageCost)
    /// - محاسبه و ذخیره TotalDifferenceValue
    /// - تغییر وضعیت سند به Approved
    /// 
    /// توجه:
    /// ثبت حسابداری این سند دیگر در این سرویس انجام نمی‌شود
    /// و از طریق AccountingService.PostInventoryAdjustmentAsync انجام خواهد شد.
    /// </summary>
    public async Task ProcessInventoryAdjustmentAsync(
        InventoryAdjustment adjustment,
        CancellationToken cancellationToken = default)
    {
        if (adjustment is null)
            throw new ArgumentNullException(nameof(adjustment));

        var branchId = currentBranch.GetRequiredBranchId();

        var dbAdjustment = await _db.InventoryAdjustments
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == adjustment.Id && x.BranchId == branchId, cancellationToken);

        if (dbAdjustment is null)
            throw new InvalidOperationException($"InventoryAdjustment with Id={adjustment.Id} not found.");

        if (dbAdjustment.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("InventoryAdjustment is already posted.");

        var moves = await _db.StockMoves
            .Include(m => m.Warehouse)
            .Where(m =>
                m.RefDocumentType == "InventoryAdjustment" &&
                m.RefDocumentId == dbAdjustment.Id &&
                m.Warehouse != null &&
                m.Warehouse.BranchId == branchId)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        if (moves.Count == 0)
            throw new InvalidOperationException("InventoryAdjustment has no related stock moves.");

        decimal totalDifferenceValue = 0m;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var move in moves)
            {
                var stockItem = await _stock.GetStockItemAsync(
                    move.WarehouseId,
                    move.ProductId,
                    cancellationToken);

                if (stockItem is null)
                {
                    stockItem = new StockItem
                    {
                        WarehouseId = move.WarehouseId,
                        ProductId = move.ProductId,
                        OnHand = 0,
                        Reserved = 0,
                        AverageCost = 0
                    };

                    await _db.StockItems.AddAsync(stockItem, cancellationToken);
                }

                if (move.MoveType == StockMoveType.Inbound ||
                    (move.MoveType == StockMoveType.Adjustment && move.Quantity > 0))
                {
                    var oldQty = stockItem.OnHand;
                    var oldCost = stockItem.AverageCost;

                    var newQty = move.Quantity;
                    var newCostPerUnit = move.UnitCost ?? oldCost;

                    var totalQty = oldQty + newQty;
                    if (totalQty > 0)
                    {
                        var totalValue = (oldQty * oldCost) + (newQty * newCostPerUnit);
                        stockItem.AverageCost = totalValue / totalQty;
                    }

                    stockItem.OnHand += newQty;

                    totalDifferenceValue += newQty * newCostPerUnit;

                    if (!move.UnitCost.HasValue)
                        move.UnitCost = stockItem.AverageCost;
                }
                else if (move.MoveType == StockMoveType.Outbound ||
                         (move.MoveType == StockMoveType.Adjustment && move.Quantity < 0))
                {
                    var qty = move.Quantity < 0 ? -move.Quantity : move.Quantity;

                    if (stockItem.OnHand < qty)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for adjustment. WarehouseId={stockItem.WarehouseId}, ProductId={stockItem.ProductId}");
                    }

                    var unitCost = stockItem.AverageCost;

                    stockItem.OnHand -= qty;

                    totalDifferenceValue -= qty * unitCost;

                    if (!move.UnitCost.HasValue)
                        move.UnitCost = unitCost;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported StockMoveType '{move.MoveType}' for inventory adjustment.");
                }

                _stock.UpdateStockItem(stockItem);
            }

            dbAdjustment.TotalDifferenceValue = totalDifferenceValue;
            dbAdjustment.Status = DocumentStatus.Approved;
            dbAdjustment.ModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
