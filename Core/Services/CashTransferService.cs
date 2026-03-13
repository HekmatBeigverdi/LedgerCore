using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.Services;

/// <summary>
/// سرویس دامین برای مدیریت انتقال وجه (بین حساب‌های بانکی / صندوق‌ها).
/// </summary>
public class CashTransferService(
    IUnitOfWork uow,
    ICurrentBranchService currentBranch,
    INumberSeriesService numberSeries,
    IPostingEngineService postingEngine) : ICashTransferService
{
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();

    private async Task<CashTransfer?> GetCashTransferScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = GetBranchIdOrThrow();
        var repo = uow.Repository<CashTransfer>();

        var page = await repo.FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            cancellationToken);

        return page.Items.FirstOrDefault();
    }

    private async Task<CashTransfer> GetCashTransferScopedOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var transfer = await GetCashTransferScopedAsync(id, cancellationToken);
        if (transfer is null)
            throw new InvalidOperationException($"CashTransfer with id={id} not found.");

        return transfer;
    }

    public async Task<CashTransfer> CreateCashTransferAsync(
        CashTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            if (transfer.Amount <= 0)
                throw new InvalidOperationException("مبلغ انتقال باید بزرگ‌تر از صفر باشد.");

            var currentBranchId = GetBranchIdOrThrow();

            if (transfer.BranchId == 0)
                transfer.BranchId = currentBranchId;
            else if (transfer.BranchId != currentBranchId)
                throw new InvalidOperationException("BranchId is not valid for current branch scope.");

            var hasFrom =
                transfer.FromBankAccountId.HasValue ||
                !string.IsNullOrWhiteSpace(transfer.FromCashDeskCode);

            var hasTo =
                transfer.ToBankAccountId.HasValue ||
                !string.IsNullOrWhiteSpace(transfer.ToCashDeskCode);

            if (!hasFrom || !hasTo)
                throw new InvalidOperationException("مبدأ و مقصد انتقال باید مشخص باشند (حساب بانکی یا صندوق).");

            if (transfer.FromBankAccountId.HasValue &&
                transfer.ToBankAccountId.HasValue &&
                transfer.FromBankAccountId == transfer.ToBankAccountId)
            {
                throw new InvalidOperationException("حساب بانکی مبدأ و مقصد نمی‌توانند یکسان باشند.");
            }

            if (transfer.FromAccountId <= 0)
                throw new InvalidOperationException("حساب حسابداری مبدأ الزامی است.");

            if (transfer.ToAccountId <= 0)
                throw new InvalidOperationException("حساب حسابداری مقصد الزامی است.");

            if (transfer.FromAccountId == transfer.ToAccountId)
                throw new InvalidOperationException("حساب حسابداری مبدأ و مقصد نمی‌توانند یکسان باشند.");

            var fromAccount = await uow.Accounts.GetByIdAsync(transfer.FromAccountId, cancellationToken)
                              ?? throw new InvalidOperationException($"Account with id={transfer.FromAccountId} not found.");

            var toAccount = await uow.Accounts.GetByIdAsync(transfer.ToAccountId, cancellationToken)
                            ?? throw new InvalidOperationException($"Account with id={transfer.ToAccountId} not found.");

            if (!fromAccount.IsActive || !fromAccount.IsPosting)
                throw new InvalidOperationException("حساب مبدأ معتبر نیست.");

            if (!toAccount.IsActive || !toAccount.IsPosting)
                throw new InvalidOperationException("حساب مقصد معتبر نیست.");

            if (string.IsNullOrWhiteSpace(transfer.Number))
            {
                transfer.Number = await numberSeries.NextAsync(
                    NumberSeriesKeys.CashTransfer,
                    transfer.BranchId,
                    cancellationToken);
            }

            transfer.Status = DocumentStatus.Draft;

            var repo = uow.Repository<CashTransfer>();
            await repo.AddAsync(transfer, cancellationToken);

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);

            return transfer;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<CashTransfer?> GetCashTransferAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetCashTransferScopedAsync(id, cancellationToken);
    }

    public async Task PostCashTransferAsync(
        int transferId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var repo = uow.Repository<CashTransfer>();
            var transfer = await GetCashTransferScopedOrThrowAsync(transferId, cancellationToken);

            if (transfer.Status == DocumentStatus.Posted)
                return;

            if (transfer.Status == DocumentStatus.Cancelled)
                throw new InvalidOperationException("سند لغو شده قابل ثبت نیست.");

            var currentBranchId = GetBranchIdOrThrow();

            if (transfer.BranchId == 0)
                transfer.BranchId = currentBranchId;
            else if (transfer.BranchId != currentBranchId)
                throw new InvalidOperationException("BranchId is not valid for current branch scope.");

            var context = new PostingContext
            {
                Total = transfer.Amount,
                CurrencyId = transfer.CurrencyId,
                FxRate = transfer.FxRate,
                Description = $"Cash transfer {transfer.Number}"
            };

            var journal = await postingEngine.BuildJournalAsync(
                documentType: "CashTransfer",
                branchId: transfer.BranchId,
                date: transfer.Date,
                refDocumentId: transfer.Id,
                refDocumentNumber: transfer.Number,
                context: context,
                cancellationToken: cancellationToken);

            await uow.Journals.AddAsync(journal, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            transfer.Status = DocumentStatus.Posted;
            transfer.JournalVoucherId = journal.Id;

            repo.Update(transfer);
            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}