using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.ViewModels.Assets;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AssetCategoriesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetCategoryDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<AssetCategory>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<AssetCategoryDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetCategoryDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<AssetCategory>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<AssetCategoryDto>).ToList();

        return Ok(new PagedResult<AssetCategoryDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<AssetCategoryDto>> Create(
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Code, request.DefaultUsefulLifeMonths, request.DefaultResidualPercent, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<AssetCategory>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await uow.Repository<AssetCategory>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<AssetCategoryDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetCategoryDto>> Update(
        int id,
        [FromBody] UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<AssetCategory>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(request.Code, request.DefaultUsefulLifeMonths, request.DefaultResidualPercent, id, cancellationToken);
        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(mapper.Map<AssetCategoryDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<AssetCategory>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<FixedAsset>()
            .AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (isUsed)
            return BadRequest("This asset category is used by fixed assets and cannot be deleted.");

        entity.IsDeleted = true;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string code,
        int usefulLifeMonths,
        decimal residualPercent,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code is required.");

        if (usefulLifeMonths <= 0)
            return BadRequest("DefaultUsefulLifeMonths must be greater than zero.");

        if (residualPercent < 0 || residualPercent > 100)
            return BadRequest("DefaultResidualPercent must be between 0 and 100.");

        var normalizedCode = code.Trim().ToUpperInvariant();

        var duplicate = await uow.Repository<AssetCategory>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Code already exists.");

        return null;
    }
}