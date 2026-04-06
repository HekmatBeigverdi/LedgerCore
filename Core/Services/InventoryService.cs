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
    
        private async Task<WarehouseTransfer?> GetWarehouseTransferScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        return await _db.WarehouseTransfers
            .Include(x => x.Lines.OrderBy(l => l.LineNumber))
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    private async Task<WarehouseTransfer> GetWarehouseTransferScopedOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var transfer = await GetWarehouseTransferScopedAsync(id, cancellationToken);
        if (transfer is null)
            throw new InvalidOperationException($"WarehouseTransfer with Id={id} not found.");

        return transfer;
    }

    private async Task<Warehouse> ValidateWarehouseAsync(
        int warehouseId,
        int branchId,
        string paramName,
        CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.BranchId == branchId, cancellationToken);

        if (warehouse is null)
            throw new InvalidOperationException($"{paramName} is invalid for current branch scope.");

        if (!warehouse.IsActive)
            throw new InvalidOperationException($"{paramName} is inactive.");

        return warehouse;
    }

    private async Task ValidateTransferLinesAsync(
        IReadOnlyList<WarehouseTransferLine> lines,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("At least one transfer line is required.");

        var lineNumber = 1;

        foreach (var line in lines)
        {
            if (line.ProductId <= 0)
                throw new InvalidOperationException("ProductId is required for all transfer lines.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException("Transfer quantity must be greater than zero.");

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == line.ProductId, cancellationToken);

            if (product is null)
                throw new InvalidOperationException($"Product with id={line.ProductId} not found.");

            if (!product.IsActive)
                throw new InvalidOperationException($"Product with id={line.ProductId} is inactive.");

            line.LineNumber = lineNumber++;
        }
    }

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
            // dbAdjustment.Status = DocumentStatus.Approved;
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
    public async Task<WarehouseTransfer> CreateWarehouseTransferAsync(
        WarehouseTransfer transfer,
        IReadOnlyList<WarehouseTransferLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (transfer is null)
            throw new ArgumentNullException(nameof(transfer));

        var branchId = currentBranch.GetRequiredBranchId();

        if (transfer.BranchId == 0)
            transfer.BranchId = branchId;
        else if (transfer.BranchId != branchId)
            throw new InvalidOperationException("BranchId is not valid for current branch scope.");

        if (transfer.FromWarehouseId <= 0 || transfer.ToWarehouseId <= 0)
            throw new InvalidOperationException("FromWarehouseId and ToWarehouseId are required.");

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
            throw new InvalidOperationException("Source and destination warehouses cannot be the same.");

        await ValidateWarehouseAsync(transfer.FromWarehouseId, branchId, nameof(transfer.FromWarehouseId), cancellationToken);
        await ValidateWarehouseAsync(transfer.ToWarehouseId, branchId, nameof(transfer.ToWarehouseId), cancellationToken);

        await ValidateTransferLinesAsync(lines, cancellationToken);

        if (string.IsNullOrWhiteSpace(transfer.Number))
        {
            transfer.Number = await numberSeries.NextAsync(
                NumberSeriesKeys.WarehouseTransfer,
                branchId,
                cancellationToken);
        }

        transfer.Date = transfer.Date == default ? DateTime.Today : transfer.Date.Date;
        transfer.Status = DocumentStatus.Draft;
        transfer.Lines = lines.ToList();

        await _db.WarehouseTransfers.AddAsync(transfer, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return transfer;
    }
    public async Task<WarehouseTransfer?> GetWarehouseTransferAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await GetWarehouseTransferScopedAsync(id, cancellationToken);
    }
        public async Task<WarehouseTransfer> UpdateWarehouseTransferAsync(
        int id,
        WarehouseTransfer request,
        IReadOnlyList<WarehouseTransferLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var branchId = currentBranch.GetRequiredBranchId();

        var entity = await GetWarehouseTransferScopedOrThrowAsync(id, cancellationToken);

        if (entity.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Only draft warehouse transfers can be edited.");

        if (request.FromWarehouseId <= 0 || request.ToWarehouseId <= 0)
            throw new InvalidOperationException("FromWarehouseId and ToWarehouseId are required.");

        if (request.FromWarehouseId == request.ToWarehouseId)
            throw new InvalidOperationException("Source and destination warehouses cannot be the same.");

        await ValidateWarehouseAsync(request.FromWarehouseId, branchId, nameof(request.FromWarehouseId), cancellationToken);
        await ValidateWarehouseAsync(request.ToWarehouseId, branchId, nameof(request.ToWarehouseId), cancellationToken);

        await ValidateTransferLinesAsync(lines, cancellationToken);

        entity.Date = request.Date == default ? entity.Date : request.Date.Date;
        entity.Description = request.Description;
        entity.FromWarehouseId = request.FromWarehouseId;
        entity.ToWarehouseId = request.ToWarehouseId;

        _db.WarehouseTransferLines.RemoveRange(entity.Lines);
        entity.Lines.Clear();

        foreach (var line in lines)
        {
            entity.Lines.Add(new WarehouseTransferLine
            {
                LineNumber = line.LineNumber,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = null,
                Description = line.Description
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }
            public async Task PostWarehouseTransferAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var transfer = await GetWarehouseTransferScopedOrThrowAsync(id, cancellationToken);

        if (transfer.Status == DocumentStatus.Posted)
            return;

        if (transfer.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Only draft warehouse transfers can be posted.");

        var branchId = currentBranch.GetRequiredBranchId();

        await ValidateWarehouseAsync(transfer.FromWarehouseId, branchId, nameof(transfer.FromWarehouseId), cancellationToken);
        await ValidateWarehouseAsync(transfer.ToWarehouseId, branchId, nameof(transfer.ToWarehouseId), cancellationToken);

        if (transfer.Lines.Count == 0)
            throw new InvalidOperationException("Warehouse transfer has no lines.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in transfer.Lines.OrderBy(x => x.LineNumber))
            {
                var sourceStock = await _stock.GetStockItemAsync(
                    transfer.FromWarehouseId,
                    line.ProductId,
                    cancellationToken);

                if (sourceStock is null || sourceStock.OnHand < line.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock in source warehouse. ProductId={line.ProductId}");
                }

                var unitCost = sourceStock.AverageCost;

                var destinationStock = await _stock.GetStockItemAsync(
                    transfer.ToWarehouseId,
                    line.ProductId,
                    cancellationToken);

                if (destinationStock is null)
                {
                    destinationStock = new StockItem
                    {
                        WarehouseId = transfer.ToWarehouseId,
                        ProductId = line.ProductId,
                        OnHand = 0,
                        Reserved = 0,
                        AverageCost = 0
                    };

                    await _db.StockItems.AddAsync(destinationStock, cancellationToken);
                }

                sourceStock.OnHand -= line.Quantity;

                var oldDestQty = destinationStock.OnHand;
                var oldDestAvg = destinationStock.AverageCost;
                var newQty = line.Quantity;

                var totalQty = oldDestQty + newQty;
                if (totalQty > 0)
                {
                    var totalValue = (oldDestQty * oldDestAvg) + (newQty * unitCost);
                    destinationStock.AverageCost = totalValue / totalQty;
                }

                destinationStock.OnHand += newQty;

                line.UnitCost = unitCost;

                await _db.StockMoves.AddAsync(new StockMove
                {
                    Date = transfer.Date,
                    WarehouseId = transfer.FromWarehouseId,
                    ProductId = line.ProductId,
                    MoveType = StockMoveType.Outbound,
                    Quantity = line.Quantity,
                    UnitCost = unitCost,
                    RefDocumentType = "WarehouseTransfer",
                    RefDocumentId = transfer.Id,
                    RefDocumentLineId = line.Id
                }, cancellationToken);

                await _db.StockMoves.AddAsync(new StockMove
                {
                    Date = transfer.Date,
                    WarehouseId = transfer.ToWarehouseId,
                    ProductId = line.ProductId,
                    MoveType = StockMoveType.Inbound,
                    Quantity = line.Quantity,
                    UnitCost = unitCost,
                    RefDocumentType = "WarehouseTransfer",
                    RefDocumentId = transfer.Id,
                    RefDocumentLineId = line.Id
                }, cancellationToken);

                _stock.UpdateStockItem(sourceStock);
                _stock.UpdateStockItem(destinationStock);
            }

            transfer.Status = DocumentStatus.Posted;
            transfer.ModifiedAt = DateTime.UtcNow;

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
