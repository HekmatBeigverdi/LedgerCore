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

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProductsController(IUnitOfWork uow) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Products_View)]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Product>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Product>(raw)
            .OrderBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new ProductDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            CategoryId = x.CategoryId,
            Barcode = x.Barcode,
            IsService = x.IsService,
            DefaultSalesPrice = x.DefaultSalesPrice,
            DefaultPurchasePrice = x.DefaultPurchasePrice,
            DefaultTaxRateId = x.DefaultTaxRateId,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Products_View)]
    public async Task<ActionResult<ProductDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new ProductDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            Barcode = entity.Barcode,
            IsService = entity.IsService,
            DefaultSalesPrice = entity.DefaultSalesPrice,
            DefaultPurchasePrice = entity.DefaultPurchasePrice,
            DefaultTaxRateId = entity.DefaultTaxRateId,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Products_Manage)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] ProductDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var repo = uow.Repository<Product>();

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("A product with this code already exists.");

        if (request.DefaultTaxRateId.HasValue)
        {
            var tax = await uow.Repository<TaxRate>().GetByIdAsync(request.DefaultTaxRateId.Value, cancellationToken);
            if (tax is null)
                return BadRequest("DefaultTaxRateId is invalid.");
        }

        var entity = new Product
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Barcode = request.Barcode,
            IsService = request.IsService,
            DefaultSalesPrice = request.DefaultSalesPrice,
            DefaultPurchasePrice = request.DefaultPurchasePrice,
            DefaultTaxRateId = request.DefaultTaxRateId,
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
    [HasPermission(PermissionCodes.Master_Products_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProductDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Product>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken);
        if (duplicateCode)
            return BadRequest("A product with this code already exists.");

        if (request.DefaultTaxRateId.HasValue)
        {
            var tax = await uow.Repository<TaxRate>().GetByIdAsync(request.DefaultTaxRateId.Value, cancellationToken);
            if (tax is null)
                return BadRequest("DefaultTaxRateId is invalid.");
        }

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.CategoryId = request.CategoryId;
        entity.Barcode = request.Barcode;
        entity.IsService = request.IsService;
        entity.DefaultSalesPrice = request.DefaultSalesPrice;
        entity.DefaultPurchasePrice = request.DefaultPurchasePrice;
        entity.DefaultTaxRateId = request.DefaultTaxRateId;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Products_Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Product>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsDeleted = true;
        entity.IsActive = false;

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}