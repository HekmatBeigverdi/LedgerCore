using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountsController(IUnitOfWork uow) : ControllerBase
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
    [HasPermission(PermissionCodes.Master_Accounts_View)]
    public async Task<ActionResult<List<AccountDto>>> GetAll(CancellationToken cancellationToken)
    {
        var raw = await uow.Repository<Account>().GetAllAsync(cancellationToken: cancellationToken);
        var items = Unwrap<Account>(raw)
            .OrderBy(x => x.Code)
            .ToList();

        var result = items.Select(x => new AccountDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Type = x.Type,
            NormalSide = x.NormalSide,
            Level = x.Level,
            IsPosting = x.IsPosting,
            ParentAccountId = x.ParentAccountId,
            RequiresParty = x.RequiresParty,
            AllowedPartyType = x.AllowedPartyType,
            IsActive = x.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_Accounts_View)]
    public async Task<ActionResult<AccountDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<Account>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return Ok(new AccountDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            NormalSide = entity.NormalSide,
            Level = entity.Level,
            IsPosting = entity.IsPosting,
            ParentAccountId = entity.ParentAccountId,
            RequiresParty = entity.RequiresParty,
            AllowedPartyType = entity.AllowedPartyType,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_Accounts_Manage)]
    public async Task<ActionResult<AccountDto>> Create(
        [FromBody] AccountDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var repo = uow.Repository<Account>();

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (duplicateCode)
            return BadRequest("An account with this code already exists.");

        if (request.ParentAccountId.HasValue)
        {
            var parent = await repo.GetByIdAsync(request.ParentAccountId.Value, cancellationToken);
            if (parent is null)
                return BadRequest("ParentAccountId is invalid.");
        }

        if (!request.RequiresParty)
            request.AllowedPartyType = null;

        var entity = new Account
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Type = request.Type,
            NormalSide = request.NormalSide,
            Level = request.Level,
            IsPosting = request.IsPosting,
            ParentAccountId = request.ParentAccountId,
            RequiresParty = request.RequiresParty,
            AllowedPartyType = request.AllowedPartyType,
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
    [HasPermission(PermissionCodes.Master_Accounts_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] AccountDto request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<Account>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (request.ParentAccountId.HasValue && request.ParentAccountId.Value == id)
            return BadRequest("An account cannot be its own parent.");

        var duplicateCode = await repo.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken);
        if (duplicateCode)
            return BadRequest("An account with this code already exists.");

        if (request.ParentAccountId.HasValue)
        {
            var parent = await repo.GetByIdAsync(request.ParentAccountId.Value, cancellationToken);
            if (parent is null)
                return BadRequest("ParentAccountId is invalid.");
        }

        var childrenRaw = await repo.FindAsync(x => x.ParentAccountId == id, cancellationToken: cancellationToken);
        var hasChildren = Unwrap<Account>(childrenRaw).Any();

        if (hasChildren && request.IsPosting)
            return BadRequest("An account with child accounts cannot be marked as posting.");

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.NormalSide = request.NormalSide;
        entity.Level = request.Level;
        entity.IsPosting = request.IsPosting;
        entity.ParentAccountId = request.ParentAccountId;
        entity.RequiresParty = request.RequiresParty;
        entity.AllowedPartyType = request.RequiresParty ? request.AllowedPartyType : null;
        entity.IsActive = request.IsActive;

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_Accounts_Manage)]
    public IActionResult Delete(int id)
    {
        return BadRequest("Deleting accounts is not allowed. Deactivate the account instead.");
    }
}