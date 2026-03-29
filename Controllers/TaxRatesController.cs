using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxRateDto = LedgerCore.Core.ViewModels.Masters.TaxRateDto;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TaxRatesController(IUnitOfWork uow) : ControllerBase
{
    private static IEnumerable<T> Unwrap<T>(object raw)
    {
        if (raw is IEnumerable<T> direct) return direct;
        if (raw is null) return Enumerable.Empty<T>();

        var type = raw.GetType();
        var prop = type.GetProperty("Items") ?? type.GetProperty("Data") ?? type.GetProperty("Results") ?? type.GetProperty("List");
        if (prop == null)
            throw new InvalidOperationException($"Returned type {type.FullName} does not expose enumerable payload.");

        var value = prop.GetValue(raw);
        return value as IEnumerable<T>
               ?? throw new InvalidOperationException($"Property {prop.Name} on {type.FullName} is not IEnumerable<{typeof(T).Name}>.");
    }

    [HttpGet]
    [HasPermission(PermissionCodes.Master_TaxRates_View)]
    public async Task<ActionResult<List<TaxRateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<TaxRate>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<TaxRate>(raw)
            .OrderBy(x => x.Name)
            .ToList();

        var result = items.Select(x => new TaxRateDto
        {
            Id = x.Id,
            Name = x.Name,
            RatePercent = x.RatePercent,
            IsIncludedInPrice = x.IsIncludedInPrice,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_TaxRates_View)]
    public async Task<ActionResult<TaxRateDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<TaxRate>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new TaxRateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RatePercent = entity.RatePercent,
            IsIncludedInPrice = entity.IsIncludedInPrice,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_TaxRates_Manage)]
    public async Task<ActionResult<TaxRateDto>> Create(
        [FromBody] TaxRateDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.RatePercent < 0)
            return BadRequest("RatePercent cannot be negative.");

        var entity = new TaxRate
        {
            Name = request.Name.Trim(),
            RatePercent = request.RatePercent,
            IsIncludedInPrice = request.IsIncludedInPrice,
            IsActive = request.IsActive
        };

        await uow.Repository<TaxRate>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        request.Id = entity.Id;
        request.Name = entity.Name;

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, request);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Master_TaxRates_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] TaxRateDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<TaxRate>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.RatePercent < 0)
            return BadRequest("RatePercent cannot be negative.");

        entity.Name = request.Name.Trim();
        entity.RatePercent = request.RatePercent;
        entity.IsIncludedInPrice = request.IsIncludedInPrice;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_TaxRates_Manage)]
    public IActionResult Delete(int id)
    {
        return BadRequest("Deleting tax rates is not allowed. Deactivate the tax rate instead.");
    }
}