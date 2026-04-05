using AutoMapper;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BankAccountsController(IUnitOfWork uow, IMapper mapper, ISecurityActivityLogService activityLog) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BankAccountDto>> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await uow.Repository<BankAccount>()
            .Query()
            .Include(x => x.Bank)
            .Include(x => x.Currency)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
            return NotFound();

        return Ok(mapper.Map<BankAccountDto>(entity));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BankAccountDto>>> Query(
        [FromQuery] PagingParams paging,
        CancellationToken cancellationToken)
    {
        var result = await uow.Repository<BankAccount>().GetAllAsync(paging, cancellationToken);
        var ids = result.Items.Select(x => x.Id).ToList();

        var fullItems = await uow.Repository<BankAccount>()
            .Query()
            .Include(x => x.Bank)
            .Include(x => x.Currency)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var items = fullItems.Select(mapper.Map<BankAccountDto>).ToList();

        return Ok(new PagedResult<BankAccountDto>(
            items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize));
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(
        [FromBody] CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(
            request.AccountNumber,
            request.Iban,
            request.BankId,
            request.CurrencyId,
            null,
            cancellationToken);

        if (validation is not null)
            return validation;

        var entity = mapper.Map<BankAccount>(request);
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.Iban = string.IsNullOrWhiteSpace(request.Iban) ? null : request.Iban.Trim().Replace(" ", "").ToUpperInvariant();
        entity.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();

        await uow.Repository<BankAccount>().AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<BankAccount>()
            .Query()
            .Include(x => x.Bank)
            .Include(x => x.Currency)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, mapper.Map<BankAccountDto>(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BankAccountDto>> Update(
        int id,
        [FromBody] UpdateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var repo = uow.Repository<BankAccount>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validation = await ValidateAsync(
            request.AccountNumber,
            request.Iban,
            request.BankId,
            request.CurrencyId,
            id,
            cancellationToken);

        if (validation is not null)
            return validation;

        mapper.Map(request, entity);
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.Iban = string.IsNullOrWhiteSpace(request.Iban) ? null : request.Iban.Trim().Replace(" ", "").ToUpperInvariant();
        entity.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();

        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        var saved = await uow.Repository<BankAccount>()
            .Query()
            .Include(x => x.Bank)
            .Include(x => x.Currency)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);

        return Ok(mapper.Map<BankAccountDto>(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<BankAccount>();
        var entity = await repo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsDeleted = true;
        entity.IsActive = false;
        repo.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);
        await activityLog.LogAsync(
            action: "BankAccount.Deleted",
            entityType: nameof(BankAccount),
            entityId: entity.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"BankAccount '{entity.BankId} - {entity.Bank!.Name}' soft-deleted.",
            cancellationToken: cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        string accountNumber,
        string? iban,
        int? bankId,
        int? currencyId,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return BadRequest("AccountNumber is required.");

        var normalizedAccountNumber = accountNumber.Trim();

        var duplicateAccount = await uow.Repository<BankAccount>()
            .AnyAsync(x =>
                x.AccountNumber == normalizedAccountNumber &&
                (!currentId.HasValue || x.Id != currentId.Value),
                cancellationToken);

        if (duplicateAccount)
            return BadRequest("AccountNumber already exists.");

        if (!string.IsNullOrWhiteSpace(iban))
        {
            var normalizedIban = iban.Trim().Replace(" ", "").ToUpperInvariant();

            var duplicateIban = await uow.Repository<BankAccount>()
                .AnyAsync(x =>
                    x.Iban != null &&
                    x.Iban == normalizedIban &&
                    (!currentId.HasValue || x.Id != currentId.Value),
                    cancellationToken);

            if (duplicateIban)
                return BadRequest("IBAN already exists.");
        }

        if (bankId.HasValue)
        {
            var bank = await uow.Repository<Bank>().GetByIdAsync(bankId.Value, cancellationToken);
            if (bank is null)
                return BadRequest("BankId is invalid.");
        }

        if (currencyId.HasValue)
        {
            var currency = await uow.Repository<Currency>().GetByIdAsync(currencyId.Value, cancellationToken);
            if (currency is null)
                return BadRequest("CurrencyId is invalid.");
        }

        return null;
    }
}