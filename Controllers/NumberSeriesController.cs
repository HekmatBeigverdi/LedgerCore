using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NumberSeriesController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Master_NumberSeries_View)]
    public async Task<ActionResult<List<NumberSeriesDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<NumberSeries>().GetAllAsync(cancellationToken: cancellationToken);

        var result = raw.Items
            .OrderBy(x => x.Code)
            .ThenBy(x => x.BranchId.HasValue ? 1 : 0)
            .ThenBy(x => x.BranchId)
            .Select(x => new NumberSeriesDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                Code = x.Code,
                BranchId = x.BranchId,
                Prefix = x.Prefix,
                Suffix = x.Suffix,
                Padding = x.Padding,
                CurrentNumber = x.CurrentNumber,
                IsActive = x.IsActive
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_NumberSeries_View)]
    public async Task<ActionResult<NumberSeriesDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<NumberSeries>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new NumberSeriesDto
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            Code = entity.Code,
            BranchId = entity.BranchId,
            Prefix = entity.Prefix,
            Suffix = entity.Suffix,
            Padding = entity.Padding,
            CurrentNumber = entity.CurrentNumber,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_NumberSeries_Manage)]
    public async Task<IActionResult> Create(
        [FromBody] NumberSeriesDto request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequestAsync(request, null, cancellationToken);
        if (validationError is not null)
            return validationError;

        var repo = uow.Repository<NumberSeries>();

        var entity = new NumberSeries
        {
            EntityType = request.EntityType.Trim(),
            Code = request.Code.Trim(),
            BranchId = request.BranchId,
            Prefix = request.Prefix?.Trim() ?? string.Empty,
            Suffix = string.IsNullOrWhiteSpace(request.Suffix) ? null : request.Suffix.Trim(),
            Padding = request.Padding,
            CurrentNumber = request.CurrentNumber,
            IsActive = request.IsActive
        };

        await repo.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        request.Id = entity.Id;
        request.EntityType = entity.EntityType;
        request.Code = entity.Code;
        request.Prefix = entity.Prefix;
        request.Suffix = entity.Suffix;

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, request);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Master_NumberSeries_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] NumberSeriesDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<NumberSeries>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validationError = await ValidateRequestAsync(request, id, cancellationToken);
        if (validationError is not null)
            return validationError;

        entity.EntityType = request.EntityType.Trim();
        entity.Code = request.Code.Trim();
        entity.BranchId = request.BranchId;
        entity.Prefix = request.Prefix?.Trim() ?? string.Empty;
        entity.Suffix = string.IsNullOrWhiteSpace(request.Suffix) ? null : request.Suffix.Trim();
        entity.Padding = request.Padding;
        entity.CurrentNumber = request.CurrentNumber;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_NumberSeries_Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<NumberSeries>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        repo.Update(entity);

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateRequestAsync(
        NumberSeriesDto request,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityType))
            return BadRequest("EntityType is required.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (request.Padding <= 0)
            return BadRequest("Padding must be greater than zero.");

        if (request.CurrentNumber < 0)
            return BadRequest("CurrentNumber cannot be negative.");

        if (request.BranchId.HasValue)
        {
            var branch = await uow.Repository<Branch>().GetByIdAsync(request.BranchId.Value, cancellationToken);
            if (branch is null)
                return BadRequest("BranchId is invalid.");
        }

        var repo = uow.Repository<NumberSeries>();

        var duplicate = await repo.AnyAsync(
            x => x.Code == request.Code &&
                 x.BranchId == request.BranchId &&
                 (!currentId.HasValue || x.Id != currentId.Value),
            cancellationToken);

        if (duplicate)
            return BadRequest("A number series with this Code and BranchId already exists.");

        return null;
    }
}