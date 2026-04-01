using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CurrenciesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CurrencyDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Currency>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<CurrencyDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CurrencyDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<Currency>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<CurrencyDto>).ToList();

        return Ok(new PagedResult<CurrencyDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create(
        [FromBody] CreateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Currency>();

        var validation = await ValidateAsync(request.Code, request.DecimalPlaces, request.IsBaseCurrency, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<Currency>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();

        await repo.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<CurrencyDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CurrencyDto>> Update(
        int id,
        [FromBody] UpdateCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Currency>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(request.Code, request.DecimalPlaces, request.IsBaseCurrency, id, cancellationToken);
        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(mapper.Map<CurrencyDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Currency>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var hasRates = await uow.Repository<ExchangeRate>()
            .AnyAsync(x => x.CurrencyId == id, cancellationToken);

        if (hasRates)
            return BadRequest("This currency has exchange rates and cannot be deleted.");

        repo.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string code,
        int decimalPlaces,
        bool isBaseCurrency,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Currency code is required.");

        if (decimalPlaces < 0 || decimalPlaces > 6)
            return BadRequest("DecimalPlaces must be between 0 and 6.");

        var normalizedCode = code.Trim().ToUpperInvariant();

        var duplicate = await uow.Repository<Currency>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Currency code already exists.");

        if (isBaseCurrency)
        {
            var anotherBase = await uow.Repository<Currency>()
                .AnyAsync(x => x.IsBaseCurrency && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

            if (anotherBase)
                return BadRequest("Only one base currency is allowed.");
        }

        return null;
    }
}