using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WarehousesController(IUnitOfWork uow) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Warehouses_View)]
    public async Task<ActionResult<List<WarehouseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Warehouse>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Warehouse>(raw)
            .OrderBy(x => x.BranchId)
            .ThenBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new WarehouseDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Address = x.Address,
            BranchId = x.BranchId,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Warehouses_View)]
    public async Task<ActionResult<WarehouseDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Warehouse>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new WarehouseDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Address = entity.Address,
            BranchId = entity.BranchId,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Warehouses_Manage)]
    public async Task<ActionResult<WarehouseDto>> Create(
        [FromBody] WarehouseDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.BranchId <= 0)
            return BadRequest("BranchId is required.");

        var branch = await uow.Repository<Branch>().GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
            return BadRequest("BranchId is invalid.");

        var repo = uow.Repository<Warehouse>();
        var duplicateCode = await repo.AnyAsync(x => x.BranchId == request.BranchId && x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("A warehouse with this code already exists in the selected branch.");

        var entity = new Warehouse
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Address = request.Address,
            BranchId = request.BranchId,
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
    [HasPermission(PermissionCodes.Master_Warehouses_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] WarehouseDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Warehouse>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.BranchId <= 0)
            return BadRequest("BranchId is required.");

        var branch = await uow.Repository<Branch>().GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
            return BadRequest("BranchId is invalid.");

        var duplicateCode = await repo.AnyAsync(
            x => x.BranchId == request.BranchId && x.Code == request.Code && x.Id != id,
            cancellationToken);

        if (duplicateCode)
            return BadRequest("A warehouse with this code already exists in the selected branch.");

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Address = request.Address;
        entity.BranchId = request.BranchId;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Warehouses_Manage)]
    public IActionResult Delete(int id)
    {
        return BadRequest("Deleting warehouses is not allowed. Deactivate the warehouse instead.");
    }
}