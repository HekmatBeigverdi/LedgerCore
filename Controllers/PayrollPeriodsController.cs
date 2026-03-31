using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.ViewModels.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayrollPeriodsController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayrollPeriodDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<PayrollPeriod>()
            .GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<PayrollPeriodDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PayrollPeriodDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<PayrollPeriod>()
            .GetAllAsync(paging, cancellationToken);

        var items = result.Items
            .Select(mapper.Map<PayrollPeriodDto>)
            .ToList();

        return Ok(new PagedResult<PayrollPeriodDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<PayrollPeriodDto>> Create(
        [FromBody] CreatePayrollPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(
            request.Code,
            request.StartDate,
            request.EndDate,
            request.FiscalPeriodId,
            null,
            cancellationToken);

        if (validation is not null)
            return validation;

        var entity = mapper.Map<PayrollPeriod>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.StartDate = request.StartDate.Date;
        entity.EndDate = request.EndDate.Date;
        entity.ClosedAt = request.IsClosed ? DateTime.UtcNow : null;

        await uow.Repository<PayrollPeriod>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<PayrollPeriod>()
            .GetByIdAsync(entity.Id, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<PayrollPeriodDto>(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PayrollPeriodDto>> Update(
        int id,
        [FromBody] UpdatePayrollPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PayrollPeriod>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(
            request.Code,
            request.StartDate,
            request.EndDate,
            request.FiscalPeriodId,
            id,
            cancellationToken);

        if (validation is not null)
            return validation;

        var wasClosed = entity.IsClosed;

        mapper.Map(request, entity);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.StartDate = request.StartDate.Date;
        entity.EndDate = request.EndDate.Date;

        if (!wasClosed && request.IsClosed)
            entity.ClosedAt = DateTime.UtcNow;
        else if (!request.IsClosed)
            entity.ClosedAt = null;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await repo.GetByIdAsync(entity.Id, cancellationToken);

        return Ok(mapper.Map<PayrollPeriodDto>(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PayrollPeriod>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<PayrollDocument>()
            .AnyAsync(x => x.PayrollPeriodId == id, cancellationToken);

        if (isUsed)
            return BadRequest("This payroll period is used by payroll documents and cannot be deleted.");

        repo.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string code,
        DateTime startDate,
        DateTime endDate,
        int? fiscalPeriodId,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code is required.");

        if (startDate == default || endDate == default)
            return BadRequest("StartDate and EndDate are required.");

        if (endDate.Date < startDate.Date)
            return BadRequest("EndDate cannot be earlier than StartDate.");

        var normalizedCode = code.Trim().ToUpperInvariant();

        var duplicateCode = await uow.Repository<PayrollPeriod>()
            .AnyAsync(
                x => x.Code == normalizedCode &&
                     (!currentId.HasValue || x.Id != currentId.Value),
                cancellationToken);

        if (duplicateCode)
            return BadRequest("Code already exists.");

        var overlap = await uow.Repository<PayrollPeriod>()
            .AnyAsync(
                x => (!currentId.HasValue || x.Id != currentId.Value) &&
                     x.StartDate.Date <= endDate.Date &&
                     x.EndDate.Date >= startDate.Date,
                cancellationToken);

        if (overlap)
            return BadRequest("This payroll period overlaps with another payroll period.");

        if (fiscalPeriodId.HasValue)
        {
            var fiscalPeriod = await uow.Repository<FiscalPeriod>()
                .GetByIdAsync(fiscalPeriodId.Value, cancellationToken);

            if (fiscalPeriod is null)
                return BadRequest("FiscalPeriodId is invalid.");

            if (startDate.Date < fiscalPeriod.StartDate.Date || endDate.Date > fiscalPeriod.EndDate.Date)
                return BadRequest("Payroll period must be within the selected fiscal period.");
        }

        return null;
    }
}