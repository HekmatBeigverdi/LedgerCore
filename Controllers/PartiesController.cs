using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyDto = LedgerCore.Core.ViewModels.Masters.PartyDto;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PartiesController(IUnitOfWork uow, ISecurityActivityLogService activityLog) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Parties_View)]
    public async Task<ActionResult<List<PartyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Party>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Party>(raw)
            .OrderBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new PartyDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Type = x.Type,
            CategoryId = x.CategoryId,
            NationalId = x.NationalId,
            EconomicCode = x.EconomicCode,
            Phone = x.Phone,
            Mobile = x.Mobile,
            Email = x.Email,
            Address = x.Address,
            City = x.City,
            CreditLimit = x.CreditLimit,
            DefaultCurrencyId = x.DefaultCurrencyId,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Parties_View)]
    public async Task<ActionResult<PartyDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Party>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new PartyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            CategoryId = entity.CategoryId,
            NationalId = entity.NationalId,
            EconomicCode = entity.EconomicCode,
            Phone = entity.Phone,
            Mobile = entity.Mobile,
            Email = entity.Email,
            Address = entity.Address,
            City = entity.City,
            CreditLimit = entity.CreditLimit,
            DefaultCurrencyId = entity.DefaultCurrencyId,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Parties_Manage)]
    public async Task<ActionResult<PartyDto>> Create(
        [FromBody] PartyDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var repo = uow.Repository<Party>();

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("A party with this code already exists.");

        if (request.DefaultCurrencyId.HasValue)
        {
            var currency = await uow.Repository<LedgerCore.Core.Models.Master.Currency>()
                .GetByIdAsync(request.DefaultCurrencyId.Value, cancellationToken);

            if (currency is null)
                return BadRequest("DefaultCurrencyId is invalid.");
        }

        var entity = new Party
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Type = request.Type,
            CategoryId = request.CategoryId,
            NationalId = request.NationalId,
            EconomicCode = request.EconomicCode,
            Phone = request.Phone,
            Mobile = request.Mobile,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            CreditLimit = request.CreditLimit,
            DefaultCurrencyId = request.DefaultCurrencyId,
            IsActive = request.IsActive
        };

        await repo.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        request.Id = entity.Id;
        request.Code = entity.Code;
        request.Name = entity.Name;

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, request);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Master_Parties_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PartyDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Party>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken);
        if (duplicateCode)
            return BadRequest("A party with this code already exists.");

        if (request.DefaultCurrencyId.HasValue)
        {
            var currency = await uow.Repository<LedgerCore.Core.Models.Master.Currency>()
                .GetByIdAsync(request.DefaultCurrencyId.Value, cancellationToken);

            if (currency is null)
                return BadRequest("DefaultCurrencyId is invalid.");
        }

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.CategoryId = request.CategoryId;
        entity.NationalId = request.NationalId;
        entity.EconomicCode = request.EconomicCode;
        entity.Phone = request.Phone;
        entity.Mobile = request.Mobile;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.City = request.City;
        entity.CreditLimit = request.CreditLimit;
        entity.DefaultCurrencyId = request.DefaultCurrencyId;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Parties_Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Party>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsDeleted = true;
        entity.IsActive = false;

        await uow.SaveChangesAsync(cancellationToken);
        await activityLog.LogAsync(
            action: "Parties.Deleted",
            entityType: nameof(Party),
            entityId: entity.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"Party '{entity.Code} - {entity.Name}' soft-deleted.",
            cancellationToken: cancellationToken);
        return NoContent();
    }
}