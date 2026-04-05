using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PartyCategoriesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartyCategoryDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<PartyCategory>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<PartyCategoryDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PartyCategoryDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<PartyCategory>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<PartyCategoryDto>).ToList();

        return Ok(new PagedResult<PartyCategoryDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<PartyCategoryDto>> Create(
        [FromBody] CreatePartyCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Code, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<PartyCategory>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await uow.Repository<PartyCategory>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<PartyCategoryDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartyCategoryDto>> Update(
        int id,
        [FromBody] UpdatePartyCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PartyCategory>();
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

        return Ok(mapper.Map<PartyCategoryDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<PartyCategory>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<Party>()
            .AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (isUsed)
            return BadRequest("This category is used by parties and cannot be deleted.");

        entity.IsDeleted = true;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);
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

        var duplicate = await uow.Repository<PartyCategory>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Code already exists.");

        return null;
    }
}