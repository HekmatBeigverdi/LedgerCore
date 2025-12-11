using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.ViewModels.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountingController(
    IAccountingService accountingService,
    IUnitOfWork uow,
    IMapper mapper)
    : ControllerBase
{
    /// <summary>
    /// لیست سندهای روزنامه با Paging ساده.
    /// </summary>
    [HttpGet("journals")]
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

        var result = await uow.Journals.QueryAsync(paging, cancellationToken);

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

    /// <summary>
    /// ایجاد سند روزنامه جدید.
    /// </summary>
    [HttpPost("journals")]
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
    public async Task<IActionResult> DeleteJournal(
        int id,
        CancellationToken cancellationToken)
    {
        await accountingService.DeleteJournalAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// پست کردن سند روزنامه.
    /// </summary>
    [HttpPost("journals/{id:int}/post")]
    public async Task<IActionResult> PostJournal(
        int id,
        CancellationToken cancellationToken)
    {
        await accountingService.PostJournalAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// بستن دوره مالی و ثبت سند اختتامیه سود و زیان.
    /// </summary>
    [HttpPost("fiscal-periods/close")]
    public async Task<IActionResult> CloseFiscalPeriod(
        [FromBody] CloseFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        await accountingService.CloseFiscalPeriodAsync(
            request.FiscalPeriodId,
            request.ProfitAndLossAccountId,
            cancellationToken);

        return NoContent();
    }
}