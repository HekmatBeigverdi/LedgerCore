using System.Linq;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Models.Settings;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountingSettingsController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Master_AccountingSettings_View)]
    public async Task<ActionResult<AccountingSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var repo = uow.Repository<AccountingSettings>();
        var raw = await repo.GetAllAsync(cancellationToken: cancellationToken);
        var entity = raw.Items.FirstOrDefault();

        if (entity is null)
            return NotFound("Accounting settings not found.");

        return Ok(new AccountingSettingsDto
        {
            Id = entity.Id,
            ReceivableAccountId = entity.ReceivableAccountId,
            PayableAccountId = entity.PayableAccountId,
            SalesRevenueAccountId = entity.SalesRevenueAccountId,
            SalesReturnAccountId = entity.SalesReturnAccountId,
            PurchaseAccountId = entity.PurchaseAccountId,
            PurchaseReturnAccountId = entity.PurchaseReturnAccountId,
            SalesVatAccountId = entity.SalesVatAccountId,
            PurchaseVatAccountId = entity.PurchaseVatAccountId,
            CashAccountId = entity.CashAccountId,
            BankAccountId = entity.BankAccountId,
            InventoryAccountId = entity.InventoryAccountId,
            CogsAccountId = entity.CogsAccountId,
            PayrollExpenseAccountId = entity.PayrollExpenseAccountId,
            PayrollPayableAccountId = entity.PayrollPayableAccountId,
            FixedAssetAccountId = entity.FixedAssetAccountId,
            AccumulatedDepreciationAccountId = entity.AccumulatedDepreciationAccountId,
            DepreciationExpenseAccountId = entity.DepreciationExpenseAccountId
        });
    }

    [HttpPut]
    [HasPermission(PermissionCodes.Master_AccountingSettings_Manage)]
    public async Task<IActionResult> Upsert(
        [FromBody] AccountingSettingsDto request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAccountsAsync(request, cancellationToken);
        if (validationError is not null)
            return validationError;

        var repo = uow.Repository<AccountingSettings>();
        var raw = await repo.GetAllAsync(cancellationToken: cancellationToken);
        var entity = raw.Items.FirstOrDefault();

        if (entity is null)
        {
            entity = new AccountingSettings
            {
                ReceivableAccountId = request.ReceivableAccountId,
                PayableAccountId = request.PayableAccountId,
                SalesRevenueAccountId = request.SalesRevenueAccountId,
                SalesReturnAccountId = request.SalesReturnAccountId,
                PurchaseAccountId = request.PurchaseAccountId,
                PurchaseReturnAccountId = request.PurchaseReturnAccountId,
                SalesVatAccountId = request.SalesVatAccountId,
                PurchaseVatAccountId = request.PurchaseVatAccountId,
                CashAccountId = request.CashAccountId,
                BankAccountId = request.BankAccountId,
                InventoryAccountId = request.InventoryAccountId,
                CogsAccountId = request.CogsAccountId,
                PayrollExpenseAccountId = request.PayrollExpenseAccountId,
                PayrollPayableAccountId = request.PayrollPayableAccountId,
                FixedAssetAccountId = request.FixedAssetAccountId,
                AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId,
                DepreciationExpenseAccountId = request.DepreciationExpenseAccountId
            };

            await repo.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ReceivableAccountId = request.ReceivableAccountId;
            entity.PayableAccountId = request.PayableAccountId;
            entity.SalesRevenueAccountId = request.SalesRevenueAccountId;
            entity.SalesReturnAccountId = request.SalesReturnAccountId;
            entity.PurchaseAccountId = request.PurchaseAccountId;
            entity.PurchaseReturnAccountId = request.PurchaseReturnAccountId;
            entity.SalesVatAccountId = request.SalesVatAccountId;
            entity.PurchaseVatAccountId = request.PurchaseVatAccountId;
            entity.CashAccountId = request.CashAccountId;
            entity.BankAccountId = request.BankAccountId;
            entity.InventoryAccountId = request.InventoryAccountId;
            entity.CogsAccountId = request.CogsAccountId;
            entity.PayrollExpenseAccountId = request.PayrollExpenseAccountId;
            entity.PayrollPayableAccountId = request.PayrollPayableAccountId;
            entity.FixedAssetAccountId = request.FixedAssetAccountId;
            entity.AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId;
            entity.DepreciationExpenseAccountId = request.DepreciationExpenseAccountId;

            repo.Update(entity);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateAccountsAsync(
        AccountingSettingsDto request,
        CancellationToken cancellationToken)
    {
        var accountRepo = uow.Repository<Account>();

        var accountIds = new[]
        {
            request.ReceivableAccountId,
            request.PayableAccountId,
            request.SalesRevenueAccountId,
            request.SalesReturnAccountId,
            request.PurchaseAccountId,
            request.PurchaseReturnAccountId,
            request.SalesVatAccountId,
            request.PurchaseVatAccountId,
            request.CashAccountId,
            request.BankAccountId,
            request.InventoryAccountId,
            request.CogsAccountId,
            request.PayrollExpenseAccountId,
            request.PayrollPayableAccountId,
            request.FixedAssetAccountId,
            request.AccumulatedDepreciationAccountId,
            request.DepreciationExpenseAccountId
        };

        if (accountIds.Any(x => x <= 0))
            return BadRequest("All accounting setting account ids must be greater than zero.");

        var duplicates = accountIds
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            return BadRequest("Duplicate account ids were found in accounting settings.");

        foreach (var accountId in accountIds)
        {
            var account = await accountRepo.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
                return BadRequest($"Account id {accountId} is invalid.");

            if (!account.IsActive)
                return BadRequest($"Account id {accountId} is inactive.");

            if (!account.IsPosting)
                return BadRequest($"Account id {accountId} must be a posting account.");
        }

        return null;
    }
}