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
using ProjectDto = LedgerCore.Core.ViewModels.Masters.ProjectDto;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProjectsController(IUnitOfWork uow) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Projects_View)]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Project>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Project>(raw)
            .OrderBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new ProjectDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Projects_View)]
    public async Task<ActionResult<ProjectDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Project>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new ProjectDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Projects_Manage)]
    public async Task<ActionResult<ProjectDto>> Create(
        [FromBody] ProjectDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            return BadRequest("StartDate cannot be greater than EndDate.");

        var repo = uow.Repository<Project>();
        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("A project with this code already exists.");

        var entity = new Project
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
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
    [HasPermission(PermissionCodes.Master_Projects_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProjectDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Project>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            return BadRequest("StartDate cannot be greater than EndDate.");

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken);
        if (duplicateCode)
            return BadRequest("A project with this code already exists.");

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Projects_Manage)]
    public IActionResult Delete(int id)
    {
        return BadRequest("Deleting projects is not allowed. Deactivate the project instead.");
    }
}