using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Workflow;
using LedgerCore.Core.ViewModels.Inventory;
using LedgerCore.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InventoryController(
    IInventoryService inventoryService,
    IAccountingService accountingService,
    IApprovalService approvalService,
    IUnitOfWork unitOfWork,
    INumberSeriesService numberSeries,
    LedgerCoreDbContext dbContext,
    ICurrentBranchService currentBranch
    )
    : ControllerBase
{
    private async Task<InventoryAdjustment?> GetAdjustmentScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        return await dbContext.InventoryAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    private async Task<InventoryAdjustment?> GetAdjustmentTrackedScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        return await dbContext.InventoryAdjustments
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }
    // ===================== Stock Info =====================

    /// <summary>
    /// کارتکس یک کالا (لیست StockMoveها) در یک انبار (اختیاری).
    /// </summary>
    [HttpGet("stock-card")]
    [HasPermission(PermissionCodes.Inventory_StockCard_View)]
    public async Task<ActionResult<IReadOnlyList<StockMove>>> GetStockCard(
        [FromQuery] int productId,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
            return BadRequest("productId is required.");

        var moves = await inventoryService.GetStockCardAsync(
            productId,
            warehouseId,
            cancellationToken);

        return Ok(moves);
    }

    /// <summary>
    /// وضعیت موجودی یک کالا در یک انبار (StockItem).
    /// </summary>
    [HttpGet("stock-item")]
    [HasPermission(PermissionCodes.Inventory_StockItem_View)]
    public async Task<ActionResult<StockItem>> GetStockItem(
        [FromQuery] int warehouseId,
        [FromQuery] int productId,
        CancellationToken cancellationToken)
    {
        if (warehouseId <= 0 || productId <= 0)
            return BadRequest("warehouseId and productId are required.");

        var item = await inventoryService.GetStockItemAsync(
            warehouseId,
            productId,
            cancellationToken);

        if (item is null)
            return NotFound();

        return Ok(item);
    }
    
    [HttpPost("adjustments/{id:int}/submit")]
    [HasPermission(PermissionCodes.Approval_Request_Create)]
    public async Task<ActionResult<ApprovalRequest>> SubmitAdjustment(
        int id,
        CancellationToken cancellationToken)
    {
        var adjustment = await GetAdjustmentTrackedScopedAsync(id, cancellationToken);
        if (adjustment is null)
            return NotFound();

        var request = await approvalService.CreateApprovalRequestAsync(
            "InventoryAdjustment",
            id,
            cancellationToken);

        return Ok(request);
    }

    // ===================== Inventory Adjustment =====================

    /// <summary>
    /// ایجاد سند تعدیل موجودی (Draft) به همراه خطوط آن (StockMoveهای Adjustment).
    /// </summary>
    [HttpPost("adjustments")]
    [HasPermission(PermissionCodes.Inventory_Adjustment_Create)]
    public async Task<ActionResult<InventoryAdjustmentDto>> CreateAdjustment(
        [FromBody] InventoryAdjustmentCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one adjustment line is required.");

        var adjustmentRepo = unitOfWork.Repository<InventoryAdjustment>();
        var stockMoveRepo = unitOfWork.Repository<StockMove>();
        
        var warehouse = await unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId, cancellationToken);
        if (warehouse is null)
            return BadRequest("Warehouse not found.");

        var currentBranchId = currentBranch.GetRequiredBranchId();

        if (warehouse.BranchId != currentBranchId)
            return BadRequest("Warehouse is not accessible in current branch scope.");

        var branchId = dto.BranchId ?? warehouse.BranchId;

        if (branchId != currentBranchId)
            return BadRequest("BranchId is not valid for current branch scope.");

        var number = string.IsNullOrWhiteSpace(dto.Number)
            ? await numberSeries.NextAsync(NumberSeriesKeys.InventoryAdjustment, branchId, cancellationToken)
            : dto.Number!;
        
        // هدر سند تعدیل
        var adjustment = new InventoryAdjustment
        {
            Number = number,
            Date = dto.Date == default ? DateTime.Today : dto.Date.Date,
            WarehouseId = dto.WarehouseId,
            BranchId = branchId,
            Description = dto.Description,
            Status = DocumentStatus.Draft
        };

        await adjustmentRepo.AddAsync(adjustment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken); // برای گرفتن Id

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

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
    [HasPermission(PermissionCodes.Inventory_Adjustment_View)]
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
    [HasPermission(PermissionCodes.Inventory_Adjustment_Process)]
    public async Task<ActionResult> ProcessAdjustment(
        int id,
        CancellationToken cancellationToken)
    {

        var adjustment = await GetAdjustmentTrackedScopedAsync(id, cancellationToken);
        if (adjustment is null)
            return NotFound();

        if (adjustment.Status == DocumentStatus.Pending)
            return BadRequest("Pending adjustment cannot be processed again.");

        if (adjustment.Status == DocumentStatus.Approved ||
            adjustment.Status == DocumentStatus.Posted)
        {
            return BadRequest("Adjustment already processed.");
        }

        await inventoryService.ProcessInventoryAdjustmentAsync(adjustment, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// ثبت سند حسابداری برای تعدیل انبار (بر اساس TotalDifferenceValue) و تغییر Status به Posted.
    /// </summary>
    [HttpPost("adjustments/{id:int}/post")]
    [HasPermission(PermissionCodes.Inventory_Adjustment_Post)]
    public async Task<ActionResult> PostAdjustmentToAccounting(
        int id,
        CancellationToken cancellationToken)
    {
        var adjustment = await GetAdjustmentTrackedScopedAsync(id, cancellationToken);

        if (adjustment is null)
            return NotFound();

        if (adjustment.Status != DocumentStatus.Approved)
        {
            return BadRequest("Only approved inventory adjustments can be posted.");
        }

        await accountingService.PostInventoryAdjustmentAsync(id, cancellationToken);

        return NoContent();
    }
    
    [HttpPost("transfers")]
    [HasPermission(PermissionCodes.Inventory_Transfer_Create)]
    public async Task<ActionResult<WarehouseTransferDto>> CreateTransfer(
        [FromBody] WarehouseTransferCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one transfer line is required.");

        var entity = new WarehouseTransfer
        {
            Number = dto.Number ?? string.Empty,
            Date = dto.Date,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            BranchId = dto.BranchId ?? 0,
            Description = dto.Description
        };

        var lines = dto.Lines.Select((x, index) => new WarehouseTransferLine
        {
            LineNumber = index + 1,
            ProductId = x.ProductId,
            Quantity = x.Quantity,
            Description = x.Description
        }).ToList();

        var created = await inventoryService.CreateWarehouseTransferAsync(
            entity,
            lines,
            cancellationToken);

        var result = await MapTransferToDtoAsync(created.Id, cancellationToken);
        return CreatedAtAction(nameof(GetTransferById), new { id = created.Id }, result);
    }
    [HttpGet("transfers/{id:int}")]
    [HasPermission(PermissionCodes.Inventory_Transfer_View)]
    public async Task<ActionResult<WarehouseTransferDto>> GetTransferById(
        int id,
        CancellationToken cancellationToken)
    {
        var dto = await MapTransferToDtoAsync(id, cancellationToken);
        if (dto is null)
            return NotFound();

        return Ok(dto);
    }
    [HttpPut("transfers/{id:int}")]
    [HasPermission(PermissionCodes.Inventory_Transfer_Edit)]
    public async Task<ActionResult<WarehouseTransferDto>> UpdateTransfer(
        int id,
        [FromBody] WarehouseTransferCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one transfer line is required.");

        var existing = await GetTransferTrackedScopedAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        var request = new WarehouseTransfer
        {
            Date = dto.Date,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            BranchId = dto.BranchId ?? existing.BranchId,
            Description = dto.Description
        };

        var lines = dto.Lines.Select((x, index) => new WarehouseTransferLine
        {
            LineNumber = index + 1,
            ProductId = x.ProductId,
            Quantity = x.Quantity,
            Description = x.Description
        }).ToList();

        await inventoryService.UpdateWarehouseTransferAsync(
            id,
            request,
            lines,
            cancellationToken);

        var result = await MapTransferToDtoAsync(id, cancellationToken);
        return Ok(result);
    }
    [HttpPost("transfers/{id:int}/post")]
    [HasPermission(PermissionCodes.Inventory_Transfer_Post)]
    public async Task<IActionResult> PostTransfer(
        int id,
        CancellationToken cancellationToken)
    {
        await inventoryService.PostWarehouseTransferAsync(id, cancellationToken);
        return NoContent();
    }

    // ===================== Helpers =====================

    private async Task<InventoryAdjustmentDto?> MapToDtoAsync(
        int id,
        CancellationToken cancellationToken)
    {
        // Use injected DbContext to load related data with EF Core.
        var context = dbContext;
        if (context == null)
        {
            return null;
        }

        var branchId = currentBranch.GetRequiredBranchId();

        var adjustment = await context.InventoryAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);

        if (adjustment is null)
            return null;

        // خطوط مربوطه (StockMoves)
        var moves = await context.StockMoves
            .AsNoTracking()
            .Include(m => m.Warehouse)
            .Where(m =>
                m.RefDocumentType == "InventoryAdjustment" &&
                m.RefDocumentId == id &&
                m.Warehouse != null &&
                m.Warehouse.BranchId == branchId)
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
    private async Task<WarehouseTransfer?> GetTransferScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        return await dbContext.WarehouseTransfers
            .AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNumber))
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }

    private async Task<WarehouseTransfer?> GetTransferTrackedScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        return await dbContext.WarehouseTransfers
            .Include(x => x.Lines.OrderBy(l => l.LineNumber))
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);
    }
    private async Task<WarehouseTransferDto?> MapTransferToDtoAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = currentBranch.GetRequiredBranchId();

        var transfer = await dbContext.WarehouseTransfers
            .AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNumber))
            .FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId, cancellationToken);

        if (transfer is null)
            return null;

        return new WarehouseTransferDto
        {
            Id = transfer.Id,
            Number = transfer.Number,
            Date = transfer.Date,
            FromWarehouseId = transfer.FromWarehouseId,
            ToWarehouseId = transfer.ToWarehouseId,
            BranchId = transfer.BranchId,
            Description = transfer.Description,
            Status = transfer.Status,
            Lines = transfer.Lines
                .OrderBy(x => x.LineNumber)
                .Select(x => new WarehouseTransferLineDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost,
                    Description = x.Description
                })
                .ToList()
        };
    }
}
