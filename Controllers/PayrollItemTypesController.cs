using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.ViewModels.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PayrollItemTypesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayrollItemTypeDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<PayrollItemType>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<PayrollItemTypeDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PayrollItemTypeDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<PayrollItemType>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<PayrollItemTypeDto>).ToList();

        return Ok(new PagedResult<PayrollItemTypeDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<PayrollItemTypeDto>> Create(
        [FromBody] CreatePayrollItemTypeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Code, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<PayrollItemType>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();

        await uow.Repository<PayrollItemType>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<PayrollItemTypeDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PayrollItemTypeDto>> Update(
        int id,
        [FromBody] UpdatePayrollItemTypeRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PayrollItemType>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(request.Code, id, cancellationToken);
        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(mapper.Map<PayrollItemTypeDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PayrollItemType>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        repo.Update(entity);

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string code,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code is required.");

        var normalizedCode = code.Trim().ToUpperInvariant();

        var duplicate = await uow.Repository<PayrollItemType>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Code already exists.");

        return null;
    }
}