using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BanksController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BankDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Bank>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<BankDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BankDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<Bank>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<BankDto>).ToList();

        return Ok(new PagedResult<BankDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<BankDto>> Create(
        [FromBody] CreateBankRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Bank name is required.");

        var entity = mapper.Map<Bank>(request);
        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();

        await uow.Repository<Bank>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<BankDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BankDto>> Update(
        int id,
        [FromBody] UpdateBankRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Bank>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Bank name is required.");

        mapper.Map(request, entity);
        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(mapper.Map<BankDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Bank>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var hasAccounts = await uow.Repository<BankAccount>()
            .AnyAsync(x => x.BankId == id, cancellationToken);

        if (hasAccounts)
            return BadRequest("This bank has bank accounts and cannot be deleted.");

        repo.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}