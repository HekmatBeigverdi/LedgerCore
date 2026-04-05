using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExchangeRatesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExchangeRateDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<ExchangeRate>()
            .Query()
            .Include(x => x.Currency)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<ExchangeRateDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExchangeRateDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<ExchangeRate>()
            .GetAllAsync(paging, cancellationToken);

        var ids = result.Items.Select(x => x.Id).ToList();

        var fullItems = await uow.Repository<ExchangeRate>()
            .Query()
            .Include(x => x.Currency)
            .Where(x => ids.Contains(x.Id))
            .OrderByDescending(x => x.RateDate)
            .ToListAsync(cancellationToken);

        var items = fullItems.Select(mapper.Map<ExchangeRateDto>).ToList();

        return Ok(new PagedResult<ExchangeRateDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeRateDto>> Create(
        [FromBody] CreateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.CurrencyId, request.RateDate, request.Rate, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<ExchangeRate>(request);
        entity.RateDate = request.RateDate.Date;

        await uow.Repository<ExchangeRate>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<ExchangeRate>()
            .Query()
            .Include(x => x.Currency)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<ExchangeRateDto>(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExchangeRateDto>> Update(
        int id,
        [FromBody] UpdateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ExchangeRate>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(request.CurrencyId, request.RateDate, request.Rate, id, cancellationToken);
        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.RateDate = request.RateDate.Date;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<ExchangeRate>()
            .Query()
            .Include(x => x.Currency)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);

        return Ok(mapper.Map<ExchangeRateDto>(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ExchangeRate>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsDeleted = true;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        int currencyId,
        DateTime rateDate,
        decimal rate,
        int? currentId,
        CancellationToken cancellationToken)
    {
        var currency = await uow.Repository<Currency>().GetByIdAsync(currencyId, cancellationToken);
        if (currency is null)
            return BadRequest("CurrencyId is invalid.");

        if (rateDate == default)
            return BadRequest("RateDate is required.");

        if (rate <= 0)
            return BadRequest("Rate must be greater than zero.");

        if (currency.IsBaseCurrency && rate != 1)
            return BadRequest("Base currency exchange rate must be 1.");

        var normalizedDate = rateDate.Date;

        var duplicate = await uow.Repository<ExchangeRate>()
            .AnyAsync(x =>
                x.CurrencyId == currencyId &&
                x.RateDate == normalizedDate &&
                (!currentId.HasValue || x.Id != currentId.Value),
                cancellationToken);

        if (duplicate)
            return BadRequest("An exchange rate already exists for this currency and date.");

        return null;
    }
}