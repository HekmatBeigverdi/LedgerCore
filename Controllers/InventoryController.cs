using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.ViewModels.Inventory;
using LedgerCore.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IAccountingService _accountingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LedgerCoreDbContext _dbContext;
    private readonly INumberSeriesService _numberSeries;


    public InventoryController(
        IInventoryService inventoryService,
        IAccountingService accountingService,
        IUnitOfWork unitOfWork,
        INumberSeriesService numberSeries,
        LedgerCoreDbContext dbContext)
    {
        _inventoryService = inventoryService;
        _accountingService = accountingService;
        _unitOfWork = unitOfWork;
        _numberSeries = numberSeries;
        _dbContext = dbContext;
    }

    // ===================== Stock Info =====================

    /// <summary>
    /// کارتکس یک کالا (لیست StockMoveها) در یک انبار (اختیاری).
    /// </summary>
    [HttpGet("stock-card")]
    // [HasPermission("Inventory.StockCard.View")]
    public async Task<ActionResult<IReadOnlyList<StockMove>>> GetStockCard(
        [FromQuery] int productId,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
            return BadRequest("productId is required.");

        var moves = await _inventoryService.GetStockCardAsync(
            productId,
            warehouseId,
            cancellationToken);

        return Ok(moves);
    }

    /// <summary>
    /// وضعیت موجودی یک کالا در یک انبار (StockItem).
    /// </summary>
    [HttpGet("stock-item")]
    // [HasPermission("Inventory.StockItem.View")]
    public async Task<ActionResult<StockItem>> GetStockItem(
        [FromQuery] int warehouseId,
        [FromQuery] int productId,
        CancellationToken cancellationToken)
    {
        if (warehouseId <= 0 || productId <= 0)
            return BadRequest("warehouseId and productId are required.");

        var item = await _inventoryService.GetStockItemAsync(
            warehouseId,
            productId,
            cancellationToken);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    // ===================== Inventory Adjustment =====================

    /// <summary>
    /// ایجاد سند تعدیل موجودی (Draft) به همراه خطوط آن (StockMoveهای Adjustment).
    /// </summary>
    [HttpPost("adjustments")]
    // [HasPermission("Inventory.Adjustment.Create")]
    public async Task<ActionResult<InventoryAdjustmentDto>> CreateAdjustment(
        [FromBody] InventoryAdjustmentCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one adjustment line is required.");

        var adjustmentRepo = _unitOfWork.Repository<InventoryAdjustment>();
        var stockMoveRepo = _unitOfWork.Repository<StockMove>();

        var number = string.IsNullOrWhiteSpace(dto.Number)
            ? await _numberSeries.NextAsync("InventoryAdjustment", dto.BranchId, cancellationToken)
            : dto.Number!;
        
        // هدر سند تعدیل
        var adjustment = new InventoryAdjustment
        {
            Number = number,
            Date = dto.Date == default ? DateTime.Today : dto.Date.Date,
            WarehouseId = dto.WarehouseId,
            BranchId = dto.BranchId,
            Description = dto.Description,
            Status = DocumentStatus.Draft
        };

        await adjustmentRepo.AddAsync(adjustment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // برای گرفتن Id

        // خطوط تعدیل به صورت StockMove (نوع Adjustment)
        foreach (var line in dto.Lines)
        {
            if (line.ProductId <= 0 || line.Quantity == 0)
                continue;

            var move = new StockMove
            {
                WarehouseId = dto.WarehouseId,
                ProductId = line.ProductId,
                Date = adjustment.Date,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                MoveType = StockMoveType.Adjustment,
                RefDocumentType = "InventoryAdjustment",
                RefDocumentId = adjustment.Id,
                //Description = line.Description ?? $"Adjustment {adjustment.Number}"
            };

            await stockMoveRepo.AddAsync(move, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await MapToDtoAsync(adjustment.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetAdjustmentById),
            new { id = adjustment.Id },
            result);
    }

    /// <summary>
    /// دریافت سند تعدیل به همراه خطوطش.
    /// </summary>
    [HttpGet("adjustments/{id:int}")]
    // [HasPermission("Inventory.Adjustment.View")]
    public async Task<ActionResult<InventoryAdjustmentDto>> GetAdjustmentById(
        int id,
        CancellationToken cancellationToken)
    {
        var dto = await MapToDtoAsync(id, cancellationToken);

        if (dto is null)
            return NotFound();

        return Ok(dto);
    }

    /// <summary>
    /// اعمال تعدیل روی موجودی انبار (محاسبه OnHand/AverageCost و TotalDifferenceValue).
    /// بعد از این مرحله، Status سند به Approved می‌رود.
    /// </summary>
    [HttpPost("adjustments/{id:int}/process")]
    // [HasPermission("Inventory.Adjustment.Process")]
    public async Task<ActionResult> ProcessAdjustment(
        int id,
        CancellationToken cancellationToken)
    {
        var adjustmentRepo = _unitOfWork.Repository<InventoryAdjustment>();

        var adjustment = await adjustmentRepo.GetByIdAsync(id, cancellationToken);
        if (adjustment is null)
            return NotFound();

        if (adjustment.Status == DocumentStatus.Approved ||
            adjustment.Status == DocumentStatus.Posted)
        {
            return BadRequest("Adjustment already processed.");
        }

        await _inventoryService.ProcessInventoryAdjustmentAsync(adjustment, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// ثبت سند حسابداری برای تعدیل انبار (بر اساس TotalDifferenceValue) و تغییر Status به Posted.
    /// </summary>
    [HttpPost("adjustments/{id:int}/post")]
    // [HasPermission("Inventory.Adjustment.Post")]
    public async Task<ActionResult> PostAdjustmentToAccounting(
        int id,
        CancellationToken cancellationToken)
    {
        var adjustmentRepo = _unitOfWork.Repository<InventoryAdjustment>();
        var adjustment = await adjustmentRepo.GetByIdAsync(id, cancellationToken);

        if (adjustment is null)
            return NotFound();

        if (adjustment.Status == DocumentStatus.Draft)
        {
            return BadRequest("Adjustment must be processed before posting to accounting.");
        }

        await _accountingService.PostInventoryAdjustmentAsync(id, cancellationToken);

        return NoContent();
    }

    // ===================== Helpers =====================

    private async Task<InventoryAdjustmentDto?> MapToDtoAsync(
        int id,
        CancellationToken cancellationToken)
    {
        // Use injected DbContext to load related data with EF Core.
        var context = _dbContext;
        if (context == null)
        {
            return null;
        }

        var adjustment = await context.InventoryAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (adjustment is null)
            return null;

        // خطوط مربوطه (StockMoves)
        var moves = await context.StockMoves
            .AsNoTracking()
            .Where(m =>
                m.RefDocumentType == "InventoryAdjustment" &&
                m.RefDocumentId == id)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        var dto = new InventoryAdjustmentDto
        {
            Id = adjustment.Id,
            Number = adjustment.Number,
            Date = adjustment.Date,
            WarehouseId = adjustment.WarehouseId,
            BranchId = adjustment.BranchId,
            Description = adjustment.Description,
            Status = adjustment.Status,
            TotalDifferenceValue = adjustment.TotalDifferenceValue,
            JournalVoucherId = adjustment.JournalVoucherId,
            Lines = moves.Select(m => new InventoryAdjustmentLineDto
            {
                ProductId = m.ProductId,
                Quantity = m.Quantity,
                UnitCost = m.UnitCost,
                //Description = m.Description
            }).ToList()
        };

        return dto;
    }
}
