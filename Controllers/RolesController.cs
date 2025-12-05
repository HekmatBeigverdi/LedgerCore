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
[Authorize(Roles = "Admin")] // فعلاً فقط Admin
public class RolesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public RolesController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // GET api/roles
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        // get roles (paged repository result -> .Items)
        var rolesPaged = await _uow.Repository<Role>()
            .FindAsync(r => true, null, cancellationToken);
        var roles = rolesPaged.Items;

        // load all role-permissions in one call and group by role id
        var allRpsPaged = await _uow.Repository<RolePermission>()
            .FindAsync(rp => true, null, cancellationToken);
        var allRps = allRpsPaged.Items;
        var permsByRole = allRps
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(rp => rp.PermissionId).ToList());

        var result = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            PermissionIds = permsByRole.TryGetValue(r.Id, out var list) ? list : new List<int>()
        }).ToList();

        return Ok(result);
    }

    // GET api/roles/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> Get(int id, CancellationToken cancellationToken)
    {
        var role = await _uow.Repository<Role>().GetByIdAsync(id, cancellationToken);
        if (role is null)
            return NotFound();

        var rpsPaged = await _uow.Repository<RolePermission>()
            .FindAsync(rp => rp.RoleId == id, null, cancellationToken);
        var permissionIds = rpsPaged.Items.Select(rp => rp.PermissionId).ToList();

        var dto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            PermissionIds = permissionIds
        };

        return Ok(dto);
    }

    // POST api/roles
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] RoleCreateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description
        };

        await _uow.Repository<Role>().AddAsync(role, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Permissionها
        if (request.PermissionIds.Any())
        {
            var rpRepo = _uow.Repository<RolePermission>();
            foreach (var pid in request.PermissionIds.Distinct())
            {
                await rpRepo.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = pid
                }, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);
        }

        var dto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            PermissionIds = request.PermissionIds.Distinct().ToList()
        };

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // PUT api/roles/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] RoleCreateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var roleRepo = _uow.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(id, cancellationToken);
        if (role is null)
            return NotFound();

        role.Name = request.Name;
        role.Description = request.Description;

        roleRepo.Update(role);
        await _uow.SaveChangesAsync(cancellationToken);

        // Update RolePermissions
        var rpRepo = _uow.Repository<RolePermission>();

        // pass null for paging params (if your FindAsync signature expects paging params as second arg)
        var existingRps = await rpRepo.FindAsync(
            rp => rp.RoleId == id,
            null,
            cancellationToken);

        // iterate the items collection from the paged result
        foreach (var rp in existingRps.Items)
            rpRepo.Remove(rp);

        if (request.PermissionIds.Any())
        {
            foreach (var pid in request.PermissionIds.Distinct())
            {
                await rpRepo.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = pid
                }, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE api/roles/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var roleRepo = _uow.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(id, cancellationToken);
        if (role is null)
            return NotFound();

        roleRepo.Remove(role);
        await _uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
