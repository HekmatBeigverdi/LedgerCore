using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class ChequeService(IUnitOfWork uow) : IChequeService
{

    /// <summary>
    /// ثبت یک چک جدید (دریافتی یا صادره).
    /// این متد فقط Cheque و ChequeHistory را ثبت می‌کند و
    /// سند حسابداری را به عهده‌ی Receipt/Payment می‌گذارد.
    /// </summary>
    public async Task<Cheque> RegisterChequeAsync(
        Cheque cheque,
        CancellationToken cancellationToken = default)
    {
        // تعیین وضعیت اولیه بر اساس نوع چک
        // دریافتی: Received
        // صادره: Issued
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
            ChangedBy = "system" // بعداً می‌توانی از کاربر لاگین شده بگیری
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
        var cheque = await uow.Cheques.GetByIdAsync(chequeId, cancellationToken);
        if (cheque is null)
            throw new InvalidOperationException($"Cheque with id={chequeId} not found.");

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

        await uow.Cheques.AddHistoryAsync(history, cancellationToken);

        // ایجاد سند حسابداری در صورت نیاز (Cleared / Returned)
        await CreateAccountingForStatusChangeAsync(cheque, newStatus, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
    }

    #region Accounting helpers

    /// <summary>
    /// برای بعضی تغییر وضعیت‌ها (Cleared, Returned) سند حسابداری می‌سازد.
    /// نوع سند و حساب‌ها از روی PostingRule تنظیم می‌شود.
    /// </summary>
    private async Task CreateAccountingForStatusChangeAsync(
        Cheque cheque,
        ChequeStatus newStatus,
        CancellationToken cancellationToken)
    {
        // فقط برای Cleared/Returned سند می‌زنیم (در این نسخه ساده)
        string? documentType = null;

        if (cheque.IsIncoming)
        {
            // چک دریافتی
            if (newStatus == ChequeStatus.Cleared)
                documentType = "ChequeIncomingCleared";
            else if (newStatus == ChequeStatus.Returned)
                documentType = "ChequeIncomingReturned";
        }
        else
        {
            // چک صادره
            if (newStatus == ChequeStatus.Cleared)
                documentType = "ChequeOutgoingCleared";
            else if (newStatus == ChequeStatus.Returned)
                documentType = "ChequeOutgoingReturned";
        }

        // برای سایر وضعیت‌ها (Received, Delivered, Cancelled) سندی ثبت نمی‌کنیم
        if (documentType is null)
            return;

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
            return;
        }
        
        var fiscalPeriodId = await GetOpenFiscalPeriodIdAsync(DateTime.UtcNow, cancellationToken);


        // ساخت سند حسابداری
        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", null, cancellationToken),
            Date = DateTime.UtcNow,
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
    }

    /// <summary>
    /// ایجاد شماره‌ی بعدی از NumberSeries برای نوع سند (مثلاً Journal)
    /// </summary>
    private async Task<string> GenerateNextNumberAsync(
        string entityType,
        int? branchId,
        CancellationToken cancellationToken)
    {
        var seriesRepo = uow.Repository<NumberSeries>();

        var page = await seriesRepo.FindAsync(
            x => x.EntityType == entityType
                 && x.IsActive
                 && (x.BranchId == null || x.BranchId == branchId),
            null,
            cancellationToken);

        var series = page.Items
            .OrderByDescending(x => x.BranchId.HasValue)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No NumberSeries defined for entityType={entityType}.");

        series.CurrentNumber += 1;
        seriesRepo.Update(series);
        await uow.SaveChangesAsync(cancellationToken);

        var num = series.CurrentNumber.ToString().PadLeft(series.Padding, '0');
        return $"{series.Prefix}{num}{series.Suffix}";
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


    #endregion
}