using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class PermissionsController(IUnitOfWork uow) : ControllerBase
{
    // GET api/permissions
    [HttpGet]
    public async Task<ActionResult<List<PermissionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Permission>();
        var raw = await repo.GetAllAsync(cancellationToken: cancellationToken);

        IEnumerable<Permission> items;
        if (raw is IEnumerable<Permission> direct)
        {
            items = direct;
        }
        else if (raw is null)
        {
            items = Enumerable.Empty<Permission>();
        }
        else
        {
            // try common wrapper property names
            var type = raw.GetType();
            var prop = type.GetProperty("Items") ?? type.GetProperty("Data") ?? type.GetProperty("Results") ?? type.GetProperty("List");
            if (prop is null)
                throw new InvalidOperationException($"GetAllAsync returned {type.FullName} which does not expose an enumerable payload.");

            var value = prop.GetValue(raw);
            items = value as IEnumerable<Permission> ?? throw new InvalidOperationException($"Property {prop.Name} on {type.FullName} is not IEnumerable<Permission>.");
        }

        var list = items.OrderBy(p => p.Code).ToList();

        var result = list.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            Description = p.Description
        }).ToList();

        return Ok(result);
    }

    // GET api/permissions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDto>> Get(int id, CancellationToken cancellationToken)
    {
        var p = await uow.Repository<Permission>().GetByIdAsync(id, cancellationToken);
        if (p is null)
            return NotFound();

        return Ok(new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            Description = p.Description
        });
    }

    // POST api/permissions
    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create(
        [FromBody] PermissionDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        var entity = new Permission
        {
            Code = request.Code,
            Description = request.Description
        };

        await uow.Repository<Permission>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        request.Id = entity.Id;
        return CreatedAtAction(nameof(Get), new { id = request.Id }, request);
    }

    // PUT api/permissions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PermissionDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Permission>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.Code = request.Code;
        entity.Description = request.Description;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE api/permissions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Permission>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        repo.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
