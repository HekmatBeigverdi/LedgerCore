using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class ChequeService(
    IUnitOfWork uow,
    ICurrentBranchService currentBranch,
    INumberSeriesService numberSeries,
    IPostingEngineService postingEngine) : IChequeService
{
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
        
        
        var actionDate = DateTime.UtcNow;

        var context = new PostingContext
        {
            Total = cheque.Amount,
            PartyId = cheque.PartyId,
            CurrencyId = cheque.CurrencyId,
            FxRate = cheque.FxRate,
            Description = $"{documentType} for cheque {cheque.ChequeNumber}"
        };

        var voucher = await postingEngine.BuildJournalAsync(
            documentType: documentType,
            branchId: cheque.BranchId,
            date: actionDate,
            refDocumentId: cheque.Id,
            refDocumentNumber: cheque.ChequeNumber,
            context: context,
            cancellationToken: cancellationToken);

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
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