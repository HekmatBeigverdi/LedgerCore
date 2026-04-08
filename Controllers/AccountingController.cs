using System.Linq;
using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Workflow;
using LedgerCore.Core.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;


namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AccountingController(
    IAccountingService accountingService,
    IApprovalService approvalService,
    IUnitOfWork uow,
    IMapper mapper,
    ISecurityActivityLogService activityLog,
    ICurrentBranchService currentBranch)
    : ControllerBase
{
    
    /// <summary>
    /// لیست سندهای روزنامه با Paging ساده.
    /// </summary>
    [HttpGet("journals")]
    [HasPermission("Accounting.Journal.View")]
    public async Task<ActionResult<PagedResult<JournalVoucherDto>>> GetJournals(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagingParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var branchId = currentBranch.GetRequiredBranchId();

        var result = await uow.Journals.QueryAsync(branchId, paging, cancellationToken);

        var items = result.Items
            .Select(j => mapper.Map<JournalVoucherDto>(j))
            .ToList();

        var dto = new PagedResult<JournalVoucherDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Ok(dto);
    }

    /// <summary>
    /// دریافت یک سند روزنامه با خطوط آن.
    /// </summary>
    [HttpGet("journals/{id:int}")]
    [HasPermission("Accounting.Journal.View")]
    public async Task<ActionResult<JournalVoucherDto>> GetJournal(
        int id,
        CancellationToken cancellationToken)
    {
        var journal = await accountingService.GetJournalAsync(id, cancellationToken);
        if (journal is null)
            return NotFound();

        var dto = mapper.Map<JournalVoucherDto>(journal);
        return Ok(dto);
    }
    
    [HttpPost("journals/{id:int}/submit")]
    [HasPermission("Approval.Request.Create")]
    public async Task<ActionResult<ApprovalRequest>> SubmitJournal(
        int id,
        CancellationToken cancellationToken)
    {
        var journal = await accountingService.GetJournalAsync(id, cancellationToken);
        if (journal is null)
            return NotFound();

        var request = await approvalService.CreateApprovalRequestAsync(
            "JournalVoucher",
            id,
            cancellationToken);

        return Ok(request);
    }

    /// <summary>
    /// ایجاد سند روزنامه جدید.
    /// </summary>
    [HttpPost("journals")]
    [HasPermission("Accounting.Journal.Create")]
    public async Task<ActionResult<JournalVoucherDto>> CreateJournal(
        [FromBody] CreateJournalVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var journal = mapper.Map<JournalVoucher>(request);
        var created = await accountingService.CreateJournalAsync(journal, cancellationToken);
        var dto = mapper.Map<JournalVoucherDto>(created);

        return CreatedAtAction(nameof(GetJournal), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// ویرایش سند روزنامه (فقط در حالت Draft).
    /// </summary>
    [HttpPut("journals/{id:int}")]
    [HasPermission("Accounting.Journal.Edit")]
    public async Task<ActionResult<JournalVoucherDto>> UpdateJournal(
        int id,
        [FromBody] UpdateJournalVoucherRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest("Route id and request id do not match.");

        var journal = mapper.Map<JournalVoucher>(request);
        var updated = await accountingService.UpdateJournalAsync(journal, cancellationToken);
        var dto = mapper.Map<JournalVoucherDto>(updated);

        return Ok(dto);
    }

    /// <summary>
    /// حذف سند روزنامه (فقط در حالت Draft).
    /// </summary>
    [HttpDelete("journals/{id:int}")]
    [HasPermission("Accounting.Journal.Delete")]
    public IActionResult DeleteJournal(int id)
    {
        return BadRequest("Deleting journal vouchers is not allowed. Use reverse for posted journals.");
    }

    /// <summary>
    /// پست کردن سند روزنامه.
    /// </summary>
    [HttpPost("journals/{id:int}/post")]
    [HasPermission("Accounting.Journal.Post")]
    public async Task<IActionResult> PostJournal(
        int id,
        CancellationToken cancellationToken)
    {
        await accountingService.PostJournalAsync(id, cancellationToken);
        return NoContent();
    }
    
    /// <summary>
    /// ایجاد سند معکوس (Reversal) برای سند روزنامه Posted.
    /// </summary>
    [HttpPost("journals/{id:int}/reverse")]
    [HasPermission("Accounting.Journal.Post")]
    public async Task<ActionResult<JournalVoucherDto>> ReverseJournal(
        int id,
        [FromBody] ReverseJournalRequest request,
        CancellationToken cancellationToken)
    {
        var reversed = await accountingService.ReverseJournalAsync(
            id,
            request.ReversalDate,
            request.Description,
            cancellationToken);

        var dto = mapper.Map<JournalVoucherDto>(reversed);
        return Ok(dto);
    }
    
    /// <summary>
    /// بستن سال مالی.
    /// </summary>
    [HttpPost("fiscal-years/close")]
    [HasPermission(PermissionCodes.Accounting_Fiscal_Close)]
    public async Task<IActionResult> CloseFiscalYear(
        [FromBody] CloseFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        await accountingService.CloseFiscalYearAsync(
            request.FiscalYearId,
            request.ProfitAndLossAccountId,
            request.CreateOpeningForNextYear,
            cancellationToken);

        await activityLog.LogAsync(
            action: "FiscalYear.Closed",
            entityType: nameof(FiscalYear),
            entityId: request.FiscalYearId,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details:
            $"ProfitAndLossAccountId={request.ProfitAndLossAccountId}, " +
            $"CreateOpeningForNextYear={request.CreateOpeningForNextYear}",
            cancellationToken: cancellationToken);

        return Ok(new { message = "Fiscal year closed successfully." });
    }

    /// <summary>
    /// بستن دوره مالی و ثبت سند اختتامیه سود و زیان.
    /// </summary>
    [HttpPost("fiscal-periods/close")]
    [HasPermission("Accounting.FiscalPeriod.Close")]
    public async Task<IActionResult> CloseFiscalPeriod(
        [FromBody] CloseFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        await accountingService.CloseFiscalPeriodAsync(
            request.FiscalPeriodId,
            request.ProfitAndLossAccountId,
            cancellationToken);

        await activityLog.LogAsync(
            action: "FiscalPeriod.Closed",
            entityType: nameof(FiscalPeriod),
            entityId: request.FiscalPeriodId,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"ProfitAndLossAccountId={request.ProfitAndLossAccountId}",
            cancellationToken: cancellationToken);

        return NoContent();
    }
    /// <summary>
    /// باز کردن (Re-open) دوره مالی. 
    /// توجه: این متد فقط فلگ IsClosed را برمی‌گرداند و سند اختتامیه را دست نمی‌زند.
    /// در صورت نیاز، منطق پاک کردن سند اختتامیه را بعداً اضافه می‌کنیم.
    /// </summary>
    [HttpPost("fiscal-periods/open")]
    [HasPermission("Accounting.FiscalPeriod.Open")]
    public async Task<IActionResult> OpenFiscalPeriod(
        [FromBody] OpenFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var periodRepo = uow.Repository<FiscalPeriod>();
        var period = await periodRepo.GetByIdAsync(request.FiscalPeriodId, cancellationToken);
        if (period is null)
            return NotFound();

        if (!period.IsClosed)
            return NoContent();

        period.IsClosed = false;
        period.ClosedAt = null;

        periodRepo.Update(period);
        await uow.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(
            action: "FiscalPeriod.Opened",
            entityType: nameof(FiscalPeriod),
            entityId: period.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"FiscalPeriod '{period.Name}' reopened.",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    // ======================== FiscalYears ========================

    [HttpGet("fiscal-years")]
    [HasPermission("Accounting.FiscalYear.View")]
    public async Task<ActionResult<IEnumerable<FiscalYearDto>>> GetFiscalYears(
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<FiscalYear>();

        var result = await repo.GetAllAsync(cancellationToken: cancellationToken);

        var dto = result.Items
            .OrderBy(y => y.StartDate)
            .Select(y => mapper.Map<FiscalYearDto>(y))
            .ToList();

        return Ok(dto);
    }

    [HttpGet("fiscal-years/{id:int}")]
    [HasPermission("Accounting.FiscalYear.View")]
    public async Task<ActionResult<FiscalYearDto>> GetFiscalYear(
        int id,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<FiscalYear>();
        var year = await repo.GetByIdAsync(id, cancellationToken);
        if (year is null)
            return NotFound();

        var dto = mapper.Map<FiscalYearDto>(year);
        return Ok(dto);
    }

    [HttpPost("fiscal-years")]
    [HasPermission("Accounting.FiscalYear.Manage")]
    public async Task<ActionResult<FiscalYearDto>> CreateFiscalYear(
        [FromBody] CreateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<FiscalYear>();

        // اینجا می‌توانی بعداً ولیدیشن هم اضافه کنی (مثلاً عدم تداخل بازه تاریخ)

        var entity = mapper.Map<FiscalYear>(request);
        await repo.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        
        await activityLog.LogAsync(
            action: "FiscalYear.Created",
            entityType: nameof(FiscalYear),
            entityId: entity.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details:
            $"Name={entity.Name}, StartDate={entity.StartDate:yyyy-MM-dd}, " +
            $"EndDate={entity.EndDate:yyyy-MM-dd}",
            cancellationToken: cancellationToken);

        var dto = mapper.Map<FiscalYearDto>(entity);

        return CreatedAtAction(nameof(GetFiscalYear), new { id = dto.Id }, dto);
    }

    [HttpPut("fiscal-years/{id:int}")]
    [HasPermission("Accounting.FiscalYear.Manage")]
    public async Task<ActionResult<FiscalYearDto>> UpdateFiscalYear(
        int id,
        [FromBody] UpdateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest("Route id and request id do not match.");

        var repo = uow.Repository<FiscalYear>();
        var existing = await repo.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        if (existing.IsClosed)
            return BadRequest("Closed fiscal year cannot be modified.");

        // Map به entity موجود
        mapper.Map(request, existing);

        repo.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);
        
        await activityLog.LogAsync(
            action: "FiscalYear.Updated",
            entityType: nameof(FiscalYear),
            entityId: existing.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details:
            $"Name={existing.Name}, StartDate={existing.StartDate:yyyy-MM-dd}, " +
            $"EndDate={existing.EndDate:yyyy-MM-dd}, IsClosed={existing.IsClosed}",
            cancellationToken: cancellationToken);

        var dto = mapper.Map<FiscalYearDto>(existing);
        return Ok(dto);
    }
    
    // ======================== FiscalPeriods ========================

    [HttpGet("fiscal-periods")]
    [HasPermission("Accounting.FiscalPeriod.View")]
    public async Task<ActionResult<IEnumerable<FiscalPeriodDto>>> GetFiscalPeriods(
        [FromQuery] int? fiscalYearId,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<FiscalPeriod>();

        IReadOnlyList<FiscalPeriod> periods;
        if (fiscalYearId.HasValue)
        {
            var result = await repo.FindAsync(
                p => p.FiscalYearId == fiscalYearId.Value,
                cancellationToken: cancellationToken);
            periods = result.Items;
        }
        else
        {
            var result = await repo.GetAllAsync(cancellationToken: cancellationToken);
            periods = result.Items;
        }

        var dto = periods
            .OrderBy(p => p.StartDate)
            .Select(p => mapper.Map<FiscalPeriodDto>(p))
            .ToList();

        return Ok(dto);
    }
    [HttpGet("fiscal-periods/{id:int}")]
    [HasPermission("Accounting.FiscalPeriod.View")]
    public async Task<ActionResult<FiscalPeriodDto>> GetFiscalPeriod(
        int id,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<FiscalPeriod>();
        var period = await repo.GetByIdAsync(id, cancellationToken);
        if (period is null)
            return NotFound();

        var dto = mapper.Map<FiscalPeriodDto>(period);
        return Ok(dto);
    }
    [HttpPost("fiscal-periods")]
    [HasPermission("Accounting.FiscalPeriod.Manage")]
    public async Task<ActionResult<FiscalPeriodDto>> CreateFiscalPeriod(
        [FromBody] CreateFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var yearRepo = uow.Repository<FiscalYear>();
        var periodRepo = uow.Repository<FiscalPeriod>();

        var fiscalYear = await yearRepo.GetByIdAsync(request.FiscalYearId, cancellationToken);
        if (fiscalYear is null)
            return BadRequest($"FiscalYear with id={request.FiscalYearId} not found.");

        if (fiscalYear.IsClosed)
            return BadRequest("Cannot create period on closed fiscal year.");

        var period = mapper.Map<FiscalPeriod>(request);

        await periodRepo.AddAsync(period, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        
        await activityLog.LogAsync(
            action: "FiscalPeriod.Created",
            entityType: nameof(FiscalPeriod),
            entityId: period.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details:
            $"FiscalYearId={period.FiscalYearId}, Name={period.Name}, " +
            $"StartDate={period.StartDate:yyyy-MM-dd}, EndDate={period.EndDate:yyyy-MM-dd}",
            cancellationToken: cancellationToken);

        var dto = mapper.Map<FiscalPeriodDto>(period);
        return CreatedAtAction(nameof(GetFiscalPeriod), new { id = dto.Id }, dto);
    }

    [HttpPut("fiscal-periods/{id:int}")]
    [HasPermission("Accounting.FiscalPeriod.Manage")]
    public async Task<ActionResult<FiscalPeriodDto>> UpdateFiscalPeriod(
        int id,
        [FromBody] UpdateFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest("Route id and request id do not match.");

        var periodRepo = uow.Repository<FiscalPeriod>();
        var yearRepo = uow.Repository<FiscalYear>();

        var existing = await periodRepo.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        if (existing.IsClosed)
            return BadRequest("Closed fiscal period cannot be modified.");

        var fiscalYear = await yearRepo.GetByIdAsync(request.FiscalYearId, cancellationToken);
        if (fiscalYear is null)
            return BadRequest($"FiscalYear with id={request.FiscalYearId} not found.");

        if (fiscalYear.IsClosed)
            return BadRequest("Cannot move period into a closed fiscal year.");

        mapper.Map(request, existing);

        periodRepo.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);
        
        await activityLog.LogAsync(
            action: "FiscalPeriod.Updated",
            entityType: nameof(FiscalPeriod),
            entityId: existing.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details:
            $"FiscalYearId={existing.FiscalYearId}, Name={existing.Name}, " +
            $"StartDate={existing.StartDate:yyyy-MM-dd}, EndDate={existing.EndDate:yyyy-MM-dd}, " +
            $"IsClosed={existing.IsClosed}",
            cancellationToken: cancellationToken);

        var dto = mapper.Map<FiscalPeriodDto>(existing);
        return Ok(dto);
    }
}