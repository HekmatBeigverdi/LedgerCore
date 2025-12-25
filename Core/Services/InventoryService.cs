using LedgerCore.Core.Interfaces.Repositories;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Core.Services;

/// <summary>
/// سرویس دامنه‌ی انبار: کارتکس، وضعیت موجودی کالا و پردازش سند تعدیل.
/// </summary>
public class InventoryService(LedgerCoreDbContext db, IStockRepository stockRepository) : IInventoryService
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
        var query = _db.StockMoves
            .AsNoTracking()
            .Where(m => m.ProductId == productId);

        if (warehouseId.HasValue)
        {
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
    public Task<StockItem?> GetStockItemAsync(
        int warehouseId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return _stock.GetStockItemAsync(warehouseId, productId, cancellationToken);
    }

    /// <summary>
    /// پردازش سند تعدیل موجودی:
    /// - پیدا کردن StockMoveهای مربوط به این Adjustment (RefDocumentType = "InventoryAdjustment")
    /// - به‌روزرسانی StockItem (OnHand و AverageCost)
    /// - محاسبه و ذخیره TotalDifferenceValue
    /// - تغییر وضعیت سند به Posted
    /// فرض: خط‌های تعدیل به صورت StockMove قبلاً ساخته شده‌اند.
    /// </summary>
    public async Task ProcessInventoryAdjustmentAsync(
        InventoryAdjustment adjustment,
        CancellationToken cancellationToken = default)
    {
        if (adjustment is null)
            throw new ArgumentNullException(nameof(adjustment));

        // اطمینان از اینکه رکورد اصلی از دیتابیس خوانده می‌شود

        var dbAdjustment = await _db.InventoryAdjustments
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == adjustment.Id, cancellationToken);

        if (dbAdjustment is null)
            throw new InvalidOperationException($"InventoryAdjustment with Id={adjustment.Id} not found.");

        if (dbAdjustment.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("InventoryAdjustment is already posted.");

        // همه‌ی حرکات انبار مربوط به این سند تعدیل
        var moves = await _db.StockMoves
            .Where(m =>
                m.RefDocumentType == "InventoryAdjustment" &&
                m.RefDocumentId == dbAdjustment.Id)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        if (moves.Count == 0)
            throw new InvalidOperationException("InventoryAdjustment has no related stock moves.");

        decimal totalDifferenceValue = 0m;

        // تراکنش EF برای اتمیک بودن عملیات
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1) Apply stock moves -> StockItem + TotalDifferenceValue (منطق فعلی شما)
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

                // منطق هم‌راستا با Purchase/Sales:
                // - Inbound یا Adjustment با مقدار مثبت: مثل خرید → محاسبه متوسط جدید
                // - Outbound یا Adjustment با مقدار منفی: مثل فروش → کاهش OnHand با AverageCost فعلی

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
            
            // 2) Create JournalVoucher for adjustment (NEW)
            if (totalDifferenceValue != 0m)
            {
                var rule = await _db.PostingRules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DocumentType == "InventoryAdjustment" && x.IsActive, cancellationToken);

                if (rule is null)
                    throw new InvalidOperationException("No posting rule defined for InventoryAdjustment.");

                var journalNumber = await GenerateNextNumberAsync("Journal", dbAdjustment.BranchId, cancellationToken);

                var abs = Math.Abs(totalDifferenceValue);

                // اگر افزایش موجودی: Debit Inventory / Credit Adjustment
                // اگر کاهش موجودی: Debit Adjustment / Credit Inventory
                var debitAccountId = totalDifferenceValue > 0 ? rule.DebitAccountId : rule.CreditAccountId;
                var creditAccountId = totalDifferenceValue > 0 ? rule.CreditAccountId : rule.DebitAccountId;

                var journal = new JournalVoucher
                {
                    Number = journalNumber,
                    Date = dbAdjustment.Date.Date,
                    BranchId = dbAdjustment.BranchId,
                    Description = $"Inventory adjustment {dbAdjustment.Number}",
                    Status = DocumentStatus.Posted,
                    Lines = new List<JournalLine>
                    {
                        new JournalLine
                        {
                            LineNumber = 1,
                            AccountId = debitAccountId,
                            Debit = abs,
                            Credit = 0,
                            RefDocumentType = "InventoryAdjustment",
                            RefDocumentId = dbAdjustment.Id,
                            Description = $"Inventory adjustment {dbAdjustment.Number}"
                        },
                        new JournalLine
                        {
                            LineNumber = 2,
                            AccountId = creditAccountId,
                            Debit = 0,
                            Credit = abs,
                            RefDocumentType = "InventoryAdjustment",
                            RefDocumentId = dbAdjustment.Id,
                            Description = $"Inventory adjustment {dbAdjustment.Number}"
                        }
                    }
                };

                await _db.JournalVouchers.AddAsync(journal, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                dbAdjustment.JournalVoucherId = journal.Id;
            }

            // 3) Finalize adjustment            

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
    // NEW helper: consistent number generation using NumberSeries table
    private async Task<string> GenerateNextNumberAsync(
        string seriesCode,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var series = await _db.NumberSeries
                         .FirstOrDefaultAsync(x => x.Code == seriesCode && x.BranchId == branchId, cancellationToken)
                     ?? await _db.NumberSeries.FirstOrDefaultAsync(x => x.Code == seriesCode && x.BranchId == null, cancellationToken);

        if (series is null)
            throw new InvalidOperationException($"NumberSeries '{seriesCode}' not found.");

        series.CurrentNumber += 1;
        series.ModifiedAt = DateTime.UtcNow;

        // نمونه ساده: Prefix + شماره با طول ثابت
        var number = $"{series.Prefix}{series.CurrentNumber.ToString().PadLeft(series.Padding, '0')}{series.Suffix}";
        _db.NumberSeries.Update(series);

        // توجه: SaveChanges بیرون انجام می‌شود
        return number;
    }
}