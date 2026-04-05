using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.ViewModels.Assets;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DepreciationMethodsController(IUnitOfWork uow, IMapper mapper, ISecurityActivityLogService activityLog) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepreciationMethodDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<DepreciationMethod>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<DepreciationMethodDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DepreciationMethodDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<DepreciationMethod>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<DepreciationMethodDto>).ToList();

        return Ok(new PagedResult<DepreciationMethodDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<DepreciationMethodDto>> Create(
        [FromBody] CreateDepreciationMethodRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Code, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<DepreciationMethod>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await uow.Repository<DepreciationMethod>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<DepreciationMethodDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepreciationMethodDto>> Update(
        int id,
        [FromBody] UpdateDepreciationMethodRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<DepreciationMethod>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(request.Code, id, cancellationToken);
        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(mapper.Map<DepreciationMethodDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<DepreciationMethod>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<FixedAsset>()
            .AnyAsync(x => x.DepreciationMethodId == id, cancellationToken);

        if (isUsed)
            return BadRequest("This depreciation method is used by fixed assets and cannot be deleted.");

        entity.IsDeleted = true;
        entity.IsActive = false;
        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);
        await activityLog.LogAsync(
            action: "DepreciationMethod.Deleted",
            entityType: nameof(DepreciationMethod),
            entityId: entity.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"Depreciation '{entity.Code} - {entity.Name}' soft-deleted.",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string code,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code is required.");

        var normalizedCode = code.Trim().ToUpperInvariant();

        var duplicate = await uow.Repository<DepreciationMethod>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Code already exists.");

        return null;
    }
}