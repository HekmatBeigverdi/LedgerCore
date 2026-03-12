using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

/// <summary>
/// سرویس دامین برای مدیریت انتقال وجه (بین حساب‌های بانکی / صندوق‌ها).
/// این سرویس فقط از IUnitOfWork، CashTransfer و NumberSeries استفاده می‌کند.
/// </summary>
public class CashTransferService(IUnitOfWork uow,
                ICurrentBranchService currentBranch,
                INumberSeriesService numberSeries)
    : ICashTransferService{
    /// <summary>
    /// ایجاد یک سند انتقال وجه جدید.
    /// </summary>
    /// Helper Methods Start
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
    private async Task<int> GetOpenFiscalPeriodIdAsync(DateTime date, CancellationToken ct)
    {
        var fyRepo = uow.Repository<FiscalYear>();
        var fyPage = await fyRepo.FindAsync(y => y.StartDate <= date && y.EndDate >= date, null, ct);

        var year = fyPage.Items
                       .OrderByDescending(y => y.StartDate)
                       .FirstOrDefault()
                   ?? throw new InvalidOperationException($"No fiscal year found for date={date:yyyy-MM-dd}.");

        if (year.IsClosed)
            throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed.");

        var fpRepo = uow.Repository<FiscalPeriod>();
        var fpPage = await fpRepo.FindAsync(
            p => p.FiscalYearId == year.Id && p.StartDate <= date && p.EndDate >= date,
            null,
            ct);

        var period = fpPage.Items
                         .OrderByDescending(p => p.StartDate)
                         .FirstOrDefault()
                     ?? throw new InvalidOperationException($"No fiscal period found for date={date:yyyy-MM-dd}.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");

        return period.Id;
    }
    private async Task<JournalVoucher> CreateJournalForCashTransferAsync(
        CashTransfer transfer,
        CancellationToken cancellationToken)
    {
        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(transfer.Date, cancellationToken);

        var voucher = new JournalVoucher
        {
            Number = await numberSeries.NextAsync(NumberSeriesKeys.Journal, transfer.BranchId, cancellationToken),
            Date = transfer.Date,
            BranchId = transfer.BranchId,
            FiscalPeriodId = fiscalPeriodId,
            Description = $"Cash transfer {transfer.Number}",
            Status = DocumentStatus.Posted,
            Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    LineNumber = 1,
                    AccountId = transfer.ToAccountId,
                    Debit = transfer.Amount,
                    Credit = 0m,
                    RefDocumentType = "CashTransfer",
                    RefDocumentId = transfer.Id,
                    CurrencyId = transfer.CurrencyId,
                    FxRate = transfer.FxRate,
                    Description = $"Cash transfer {transfer.Number} - debit destination"
                },
                new JournalLine
                {
                    LineNumber = 2,
                    AccountId = transfer.FromAccountId,
                    Debit = 0m,
                    Credit = transfer.Amount,
                    RefDocumentType = "CashTransfer",
                    RefDocumentId = transfer.Id,
                    CurrencyId = transfer.CurrencyId,
                    FxRate = transfer.FxRate,
                    Description = $"Cash transfer {transfer.Number} - credit source"
                }
            }
        };

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }
    /// Helper Methods End

    public async Task<CashTransfer> CreateCashTransferAsync(
        CashTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // اعتبارسنجی‌های پایه
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

            // اگر شماره خالی است، از NumberSeries بساز
            if (string.IsNullOrWhiteSpace(transfer.Number))
            {
                transfer.Number = await numberSeries.NextAsync(
                    NumberSeriesKeys.CashTransfer,
                    transfer.BranchId,
                    cancellationToken);
            }

            // وضعیت اولیه
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

    /// <summary>
    /// دریافت یک سند انتقال وجه بر اساس Id.
    /// </summary>
    public Task<CashTransfer?> GetCashTransferAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetCashTransferScopedAsync(id, cancellationToken);
    }

    /// <summary>
    /// ثبت (Post) سند انتقال وجه.
    /// فعلاً فقط وضعیت را Posted می‌کند؛
    /// در گام بعدی می‌توانیم ایجاد JournalVoucher را هم اضافه کنیم.
    /// </summary>
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

            var journal = await CreateJournalForCashTransferAsync(transfer, cancellationToken);

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