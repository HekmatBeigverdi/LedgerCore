using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class ChequeService(IUnitOfWork uow,
                ICurrentBranchService currentBranch,
                INumberSeriesService numberSeries)
    : IChequeService{
    
    
    /// Helper Methods Start
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();

    private async Task<Cheque?> GetChequeScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = GetBranchIdOrThrow();
        var page = await uow.Repository<Cheque>().FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            cancellationToken);

        return page.Items.FirstOrDefault();
    }

    private async Task<Cheque> GetChequeScopedOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var cheque = await GetChequeScopedAsync(id, cancellationToken);
        if (cheque is null)
            throw new InvalidOperationException($"Cheque with id={id} not found.");

        return cheque;
    }
    /// Helper Methods End

    /// <summary>
    /// ثبت یک چک جدید (دریافتی یا صادره).
    /// این متد فقط Cheque و ChequeHistory را ثبت می‌کند و
    /// سند حسابداری را به عهده‌ی Receipt/Payment می‌گذارد.
    /// </summary>
    public async Task<Cheque> RegisterChequeAsync(
        Cheque cheque,
        CancellationToken cancellationToken = default)
    {
        var currentBranchId = GetBranchIdOrThrow();

        if (cheque.BranchId == 0)
            cheque.BranchId = currentBranchId;
        else if (cheque.BranchId != currentBranchId)
            throw new InvalidOperationException("BranchId is not valid for current branch scope.");

        cheque.Status = cheque.IsIncoming
            ? ChequeStatus.Received
            : ChequeStatus.Issued;

        await uow.Cheques.AddAsync(cheque, cancellationToken);

        var history = new ChequeHistory
        {
            Cheque = cheque,
            ChangeDate = DateTime.UtcNow,
            Status = cheque.Status,
            Description = cheque.Description,
            ChangedBy = "system"
        };

        await uow.Cheques.AddHistoryAsync(history, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return cheque;
    }

    /// <summary>
    /// تغییر وضعیت چک + ثبت در تاریخچه + در صورت نیاز، ایجاد سند حسابداری.
    /// </summary>
    public async Task ChangeStatusAsync(
        int chequeId,
        ChequeStatus newStatus,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var cheque = await GetChequeScopedOrThrowAsync(chequeId, cancellationToken);

            ValidateStatusTransition(cheque, newStatus);

            // تغییر وضعیت
            cheque.Status = newStatus;
            uow.Cheques.Update(cheque);

            // ثبت در تاریخچه
            var history = new ChequeHistory
            {
                ChequeId = cheque.Id,
                ChangeDate = DateTime.UtcNow,
                Status = newStatus,
                Description = comment,
                ChangedBy = "system"
            };

            var journal = await CreateAccountingForStatusChangeAsync(
                cheque,
                newStatus,
                cancellationToken);

            if (journal is not null)
            {
                history.JournalVoucherId = journal.Id;
            }

            await uow.Cheques.AddHistoryAsync(history, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    public Task<Cheque?> GetChequeAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return GetChequeScopedAsync(id, cancellationToken);
    }
    public async Task<IReadOnlyList<Cheque>> GetByStatusAsync(
        ChequeStatus status,
        CancellationToken cancellationToken = default)
    {
        var branchId = GetBranchIdOrThrow();

        var page = await uow.Repository<Cheque>().FindAsync(
            x => x.Status == status && x.BranchId == branchId,
            null,
            cancellationToken);

        return page.Items.ToList();
    }

    #region Accounting helpers

    /// <summary>
    /// برای بعضی تغییر وضعیت‌ها (Delivered, Cleared, Returned) سند حسابداری می‌سازد.
    /// نوع سند و حساب‌ها از روی PostingRule تنظیم می‌شود.
    /// </summary>
    private async Task<JournalVoucher?> CreateAccountingForStatusChangeAsync(
        Cheque cheque,
        ChequeStatus newStatus,
        CancellationToken cancellationToken)
    {
        // فقط برای Delivered / Cleared / Returned سند می‌زنیم
        string? documentType = null;

        if (cheque.IsIncoming)
        {
            // چک دریافتی
            if (newStatus == ChequeStatus.Delivered)
                documentType = "ChequeIncomingDelivered";
            else if (newStatus == ChequeStatus.Cleared)
                documentType = "ChequeIncomingCleared";
            else if (newStatus == ChequeStatus.Returned)
                documentType = "ChequeIncomingReturned";
        }
        else
        {
            // چک صادره
            if (newStatus == ChequeStatus.Delivered)
                documentType = "ChequeOutgoingDelivered";
            else if (newStatus == ChequeStatus.Cleared)
                documentType = "ChequeOutgoingCleared";
            else if (newStatus == ChequeStatus.Returned)
                documentType = "ChequeOutgoingReturned";
        }

        // برای سایر وضعیت‌ها (مثل Received / Issued / Cancelled) سندی ثبت نمی‌کنیم
        if (documentType is null)
            return null;

        // خواندن PostingRule متناسب با این نوع
        var postingRuleRepo = uow.Repository<PostingRule>();
        var page = await postingRuleRepo.FindAsync(
            x => x.DocumentType == documentType && x.IsActive,
            null,
            cancellationToken);

        var rule = page.Items.FirstOrDefault();
        if (rule is null)
        {
            // اگر قاعده‌ای تعریف نشده، از نظر سیستمی می‌توانیم:
            // - هیچ سندی نزنیم (return)
            // - یا خطا بدهیم
            // در اینجا برای نرم‌تر بودن رفتار، فقط return می‌کنیم.
            return null;
        }
        
        var actionDate = DateTime.UtcNow;
        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(actionDate, cancellationToken);

        // ساخت سند حسابداری
        var voucher = new JournalVoucher
        {
            BranchId = cheque.BranchId,
            Number = await numberSeries.NextAsync(NumberSeriesKeys.Journal, cheque.BranchId, cancellationToken),
            Date = actionDate,
            FiscalPeriodId = fiscalPeriodId,
            Description = $"{documentType} for cheque {cheque.ChequeNumber}",
            Status = DocumentStatus.Posted
        };

        var lines = new List<JournalLine>();
        int lineNo = 1;

        // بدهکار
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.DebitAccountId,
            Debit = cheque.Amount,
            Credit = 0,
            RefDocumentType = "Cheque",
            RefDocumentId = cheque.Id,
            CurrencyId = cheque.CurrencyId,
            FxRate = cheque.FxRate,
            Description = $"{documentType} - Debit"
        });

        // بستانکار
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.CreditAccountId,
            Debit = 0,
            Credit = cheque.Amount,
            RefDocumentType = "Cheque",
            RefDocumentId = cheque.Id,
            CurrencyId = cheque.CurrencyId,
            FxRate = cheque.FxRate,
            Description = $"{documentType} - Credit"
        });

        voucher.Lines = lines;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        
        return voucher;
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
    
    private static void ValidateStatusTransition(Cheque cheque, ChequeStatus newStatus)
    {
        var current = cheque.Status;

        if (current == newStatus)
            return;

        if (cheque.IsIncoming)
        {
            var allowed = current switch
            {
                ChequeStatus.Received => newStatus is ChequeStatus.Delivered or ChequeStatus.Returned or ChequeStatus.Cancelled,
                ChequeStatus.Delivered => newStatus is ChequeStatus.Cleared or ChequeStatus.Returned or ChequeStatus.Cancelled,
                ChequeStatus.Returned => false,
                ChequeStatus.Cleared => false,
                ChequeStatus.Cancelled => false,
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException($"Invalid incoming cheque transition: {current} -> {newStatus}");
        }
        else
        {
            var allowed = current switch
            {
                ChequeStatus.Issued => newStatus is ChequeStatus.Delivered or ChequeStatus.Cancelled,
                ChequeStatus.Delivered => newStatus is ChequeStatus.Cleared or ChequeStatus.Returned or ChequeStatus.Cancelled,
                ChequeStatus.Returned => false,
                ChequeStatus.Cleared => false,
                ChequeStatus.Cancelled => false,
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException($"Invalid outgoing cheque transition: {current} -> {newStatus}");
        }
    }


    #endregion
}