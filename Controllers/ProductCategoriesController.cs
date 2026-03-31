using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductCategoriesController(IUnitOfWork uow, IMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductCategoryDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<ProductCategory>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<ProductCategoryDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductCategoryDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<ProductCategory>().GetAllAsync(paging, cancellationToken);
        var items = result.Items.Select(mapper.Map<ProductCategoryDto>).ToList();

        return Ok(new PagedResult<ProductCategoryDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<ProductCategoryDto>> Create(
        [FromBody] CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request.Code, null, cancellationToken);
        if (validation is not null)
            return validation;

        var entity = mapper.Map<ProductCategory>(request);
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await uow.Repository<ProductCategory>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<ProductCategoryDto>(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductCategoryDto>> Update(
        int id,
        [FromBody] UpdateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ProductCategory>();
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

        return Ok(mapper.Map<ProductCategoryDto>(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<ProductCategory>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var isUsed = await uow.Repository<Product>()
            .AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (isUsed)
            return BadRequest("This category is used by products and cannot be deleted.");

        repo.Remove(entity);
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

        var duplicate = await uow.Repository<ProductCategory>()
            .AnyAsync(x => x.Code == normalizedCode && (!currentId.HasValue || x.Id != currentId.Value), cancellationToken);

        if (duplicate)
            return BadRequest("Code already exists.");

        return null;
    }
}