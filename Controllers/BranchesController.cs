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
using BranchDto = LedgerCore.Core.ViewModels.Masters.BranchDto;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BranchesController(IUnitOfWork uow) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Branches_View)]
    public async Task<ActionResult<List<BranchDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Branch>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Branch>(raw)
            .OrderBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new BranchDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Address = x.Address,
            Phone = x.Phone,
            IsHeadOffice = x.IsHeadOffice,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Branches_View)]
    public async Task<ActionResult<BranchDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Branch>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new BranchDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Address = entity.Address,
            Phone = entity.Phone,
            IsHeadOffice = entity.IsHeadOffice,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Branches_Manage)]
    public async Task<ActionResult<BranchDto>> Create(
        [FromBody] BranchDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var repo = uow.Repository<Branch>();

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("A branch with this code already exists.");

        var entity = new Branch
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Address = request.Address,
            Phone = request.Phone,
            IsHeadOffice = request.IsHeadOffice,
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
    [HasPermission(PermissionCodes.Master_Branches_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BranchDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Branch>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken);
        if (duplicateCode)
            return BadRequest("A branch with this code already exists.");

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Address = request.Address;
        entity.Phone = request.Phone;
        entity.IsHeadOffice = request.IsHeadOffice;
        entity.IsActive = request.IsActive;
        

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Branches_Manage)]
    public IActionResult Delete(int id)
    {
        return BadRequest("Deleting branches is not allowed. Deactivate the branch instead.");
    }
}