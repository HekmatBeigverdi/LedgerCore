using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class AccountingService(
    IUnitOfWork uow,
    IReportingService reportingService) : IAccountingService
{
    // ===================== ژورنال =====================
    
    
    private async Task<FiscalYear> GetFiscalYearByDateOrThrowAsync(
        DateTime date,
        CancellationToken ct)
    {
        var fyRepo = uow.Repository<FiscalYear>();

        var page = await fyRepo.FindAsync(
            y => y.StartDate <= date && y.EndDate >= date,
            null,
            ct);

        var year = page.Items
            .OrderByDescending(y => y.StartDate)
            .FirstOrDefault();

        if (year is null)
            throw new InvalidOperationException($"No fiscal year found for date={date:yyyy-MM-dd}.");

        if (year.IsClosed)
            throw new InvalidOperationException($"Fiscal year '{year.Name}' is closed.");

        return year;
    }

    private async Task<FiscalPeriod> GetOpenFiscalPeriodAsync(
        DateTime date,
        int? expectedFiscalPeriodId,
        CancellationToken ct)
    {
        var year = await GetFiscalYearByDateOrThrowAsync(date, ct);

        var fpRepo = uow.Repository<FiscalPeriod>();
        var page = await fpRepo.FindAsync(
            p => p.FiscalYearId == year.Id && p.StartDate <= date && p.EndDate >= date,
            null,
            ct);

        var period = page.Items
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();

        if (period is null)
            throw new InvalidOperationException($"No fiscal period found for date={date:yyyy-MM-dd} in fiscal year '{year.Name}'.");

        if (period.IsClosed)
            throw new InvalidOperationException($"Fiscal period '{period.Name}' is closed.");

        if (expectedFiscalPeriodId.HasValue && expectedFiscalPeriodId.Value != period.Id)
            throw new InvalidOperationException("FiscalPeriodId does not match the provided Date.");

        return period;
    }

    public async Task<JournalVoucher> CreateJournalAsync(
        JournalVoucher voucher,
        CancellationToken cancellationToken = default)
    {
        // اگر شماره ندارد، از NumberSeries بگیریم
        if (string.IsNullOrWhiteSpace(voucher.Number))
        {
            voucher.Number = await GenerateNextNumberAsync("Journal", voucher.BranchId, cancellationToken);
        }
        
        
        // Fiscal lock + تعیین دوره
        var period = await GetOpenFiscalPeriodAsync(voucher.Date, voucher.FiscalPeriodId, cancellationToken);
        voucher.FiscalPeriodId = period.Id;
        
        
        // اعتبارسنجی قواعد ژورنال + تفصیلی
        await ValidateJournalVoucherAsync(voucher, cancellationToken);

        // اعتبارسنجی تراز بودن
        if (!IsBalanced(voucher))
            throw new InvalidOperationException("Journal voucher is not balanced (Debit != Credit).");

        voucher.Status = DocumentStatus.Draft;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    public Task<JournalVoucher?> GetJournalAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return uow.Journals.GetWithLinesAsync(id, cancellationToken);
    }
    
    public async Task<JournalVoucher> UpdateJournalAsync(
        JournalVoucher voucher,
        CancellationToken cancellationToken = default)
    {
        var existing = await uow.Journals.GetWithLinesAsync(voucher.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"JournalVoucher with id={voucher.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted journal cannot be updated.");

        // هدر سند را به‌روزرسانی می‌کنیم
        existing.Date = voucher.Date;
        existing.Description = voucher.Description;
        existing.BranchId = voucher.BranchId;
        existing.FiscalPeriodId = voucher.FiscalPeriodId;

        // سطرها را ساده ری‌بیلد می‌کنیم
        existing.Lines.Clear();
        foreach (var line in voucher.Lines)
        {
            existing.Lines.Add(new JournalLine
            {
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                PartyId = line.PartyId,
                CostCenterId = line.CostCenterId,
                ProjectId = line.ProjectId,
                CurrencyId = line.CurrencyId,
                FxRate = line.FxRate,
                Description = line.Description,
                RefDocumentType = line.RefDocumentType,
                RefDocumentId = line.RefDocumentId,
                LineNumber = line.LineNumber
            });
        }
        
        var period = await GetOpenFiscalPeriodAsync(existing.Date, voucher.FiscalPeriodId, cancellationToken);
        existing.FiscalPeriodId = period.Id;

        
        // اعتبارسنجی قواعد ژورنال + تفصیلی
        await ValidateJournalVoucherAsync(existing, cancellationToken);

        if (!IsBalanced(existing))
            throw new InvalidOperationException("Journal voucher is not balanced (Debit != Credit).");

        uow.Journals.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteJournalAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var existing = await uow.Journals.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"JournalVoucher with id={id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted journal cannot be deleted.");

        uow.Journals.Remove(existing);
        await uow.SaveChangesAsync(cancellationToken);
    }
    
    public async Task PostJournalAsync(
        int journalId,
        CancellationToken cancellationToken = default)
    {
        
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var journal = await uow.Journals.GetWithLinesAsync(journalId, cancellationToken);
            if (journal is null)
                throw new InvalidOperationException($"JournalVoucher with id={journalId} not found.");

            if (journal.Status == DocumentStatus.Posted)
                return;
            
            var period = await GetOpenFiscalPeriodAsync(journal.Date, journal.FiscalPeriodId, cancellationToken);
            journal.FiscalPeriodId = period.Id;
            
            // اعتبارسنجی قواعد ژورنال + تفصیلی (قبل از Post)
            await ValidateJournalVoucherAsync(journal, cancellationToken);

            if (!IsBalanced(journal))
                throw new InvalidOperationException("Cannot post unbalanced journal.");

            journal.Status = DocumentStatus.Posted;
            uow.Journals.Update(journal);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<JournalVoucher> ReverseJournalAsync(
        int journalId,
        DateTime? reversalDate = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // اصل سند
        var original = await uow.Journals.GetWithLinesAsync(journalId, cancellationToken);
        if (original is null)
            throw new InvalidOperationException($"JournalVoucher with id={journalId} not found.");

        if (original.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("Only a posted journal can be reversed.");

        var revDate = (reversalDate ?? DateTime.UtcNow).Date;

        // Fiscal lock برای تاریخ ریورس
        var period = await GetOpenFiscalPeriodAsync(revDate, null, cancellationToken);

        // ساخت سند معکوس
        var reversed = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", original.BranchId, cancellationToken),
            Date = revDate,
            BranchId = original.BranchId,
            FiscalPeriodId = period.Id,
            Description = description ?? $"Reversal of JV {original.Number} (id={original.Id})",
            Status = DocumentStatus.Draft,
            Lines = new List<JournalLine>()
        };

        var lineNo = 1;
        foreach (var l in original.Lines.OrderBy(x => x.LineNumber))
        {
            reversed.Lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = l.AccountId,
                Debit = l.Credit,
                Credit = l.Debit,
                PartyId = l.PartyId,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId,
                CurrencyId = l.CurrencyId,
                FxRate = l.FxRate,
                RefDocumentType = "JournalVoucher",
                RefDocumentId = original.Id,
                Description = $"Reversal line of {original.Number}"
            });
        }

        // اعتبارسنجی و ذخیره
        await ValidateJournalVoucherAsync(reversed, cancellationToken);

        if (!IsBalanced(reversed))
            throw new InvalidOperationException("Reversal journal is not balanced.");

        await uow.Journals.AddAsync(reversed, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        // پست کردن سند معکوس (از همان مسیر استاندارد)
        await PostJournalAsync(reversed.Id, cancellationToken);

        // گرفتن نسخه نهایی با خطوط
        var final = await uow.Journals.GetWithLinesAsync(reversed.Id, cancellationToken);
        return final ?? reversed;
    }

    
    
    public async Task CloseFiscalPeriodAsync(
        int fiscalPeriodId,
        int profitAndLossAccountId,
        CancellationToken cancellationToken = default)
    {
        var periodRepo = uow.Repository<FiscalPeriod>();
        var period = await periodRepo.GetByIdAsync(fiscalPeriodId, cancellationToken)
                     ?? throw new InvalidOperationException($"FiscalPeriod with id={fiscalPeriodId} not found.");

        if (period.IsClosed)
            return;

        // 1) گرفتن تراز آزمایشی دوره از ReportingService
        var trialBalance = await reportingService.GetTrialBalanceAsync(
            period.StartDate,
            period.EndDate,
            branchId: null,
            cancellationToken);

        // اگر هیچ حرکت درآمد/هزینه‌ای نداریم، فقط دوره را ببند
        if (trialBalance == null || trialBalance.Count == 0)
        {
            period.IsClosed = true;
            period.ClosedAt = DateTime.UtcNow;
            periodRepo.Update(period);
            await uow.SaveChangesAsync(cancellationToken);
            return;
        }

        // 2) چک کردن حساب سود و زیان (ِEquity)
        var plAccount = await uow.Accounts.GetByIdAsync(profitAndLossAccountId, cancellationToken)
                        ?? throw new InvalidOperationException($"Account with id={profitAndLossAccountId} not found.");

        if (plAccount.Type != AccountType.Equity)
            throw new InvalidOperationException("Profit and loss account must be an equity account.");

        var lines = new List<JournalLine>();
        var lineNumber = 1;
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;

        // 3) برای همه حساب‌های درآمد و هزینه، مانده دوره را صفر می‌کنیم
        foreach (var row in trialBalance)
        {
            var account = await uow.Accounts.GetByIdAsync(row.AccountId, cancellationToken);
            if (account is null)
                continue;

            if (account.Type != AccountType.Revenue && account.Type != AccountType.Expense)
                continue;

            var net = row.PeriodDebit - row.PeriodCredit;
            if (net == 0m)
                continue;

            if (net > 0m)
            {
                // مانده بدهکار (هزینه) => بستن با ثبت بستانکار
                lines.Add(new JournalLine
                {
                    AccountId = row.AccountId,
                    Debit = 0m,
                    Credit = net,
                    LineNumber = lineNumber++,
                    Description = "Closing entry"
                });
                totalCredit += net;
            }
            else
            {
                var amount = Math.Abs(net);
                // مانده بستانکار (درآمد) => بستن با ثبت بدهکار
                lines.Add(new JournalLine
                {
                    AccountId = row.AccountId,
                    Debit = amount,
                    Credit = 0m,
                    LineNumber = lineNumber++,
                    Description = "Closing entry"
                });
                totalDebit += amount;
            }
        }

        if (!lines.Any())
        {
            // هیچ حساب سود و زیانی برای بستن نداریم
            period.IsClosed = true;
            period.ClosedAt = DateTime.UtcNow;
            periodRepo.Update(period);
            await uow.SaveChangesAsync(cancellationToken);
            return;
        }

        // 4) ثبت سطر خلاصه سود و زیان در حساب profitAndLossAccountId
        var netPl = totalDebit - totalCredit;
        if (netPl > 0m)
        {
            // خالص بدهکار (زیان) => بستانکار کردن حساب سود و زیان
            lines.Add(new JournalLine
            {
                AccountId = profitAndLossAccountId,
                Debit = 0m,
                Credit = netPl,
                LineNumber = lineNumber++,
                Description = $"Net loss closing for period {period.Name}"
            });
            totalCredit += netPl;
        }
        else if (netPl < 0m)
        {
            var amount = Math.Abs(netPl);
            // خالص بستانکار (سود) => بدهکار کردن حساب سود و زیان
            lines.Add(new JournalLine
            {
                AccountId = profitAndLossAccountId,
                Debit = amount,
                Credit = 0m,
                LineNumber = lineNumber++,
                Description = $"Net profit closing for period {period.Name}"
            });
            totalDebit += amount;
        }

        // در این مرحله مجموع بدهکار و بستانکار باید برابر باشد، اختلاف‌های خیلی کوچک را می‌توانی بعداً هندل کنی

        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var closingVoucher = new JournalVoucher
            {
                Number = await GenerateNextNumberAsync("ClosingJournal", null, cancellationToken),
                Date = period.EndDate,
                Description = $"Closing entries for fiscal period {period.Name}",
                Status = DocumentStatus.Posted,
                FiscalPeriodId = fiscalPeriodId,
                Lines = lines
            };

            await uow.Journals.AddAsync(closingVoucher, cancellationToken);

            period.IsClosed = true;
            period.ClosedAt = DateTime.UtcNow;
            periodRepo.Update(period);

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task CloseFiscalYearAsync(
        int fiscalYearId,
        int profitAndLossAccountId,
        bool createOpeningForNextYear = true,
        CancellationToken cancellationToken = default)
    {
        var fyRepo = uow.Repository<FiscalYear>();
        var periodRepo = uow.Repository<FiscalPeriod>();

        var fiscalYear = await fyRepo.GetByIdAsync(fiscalYearId, cancellationToken)
                         ?? throw new InvalidOperationException($"FiscalYear with id={fiscalYearId} not found.");

        if (fiscalYear.IsClosed)
            return;

        // 1) لیست دوره‌های سال
        var periodsResult = await periodRepo.FindAsync(
            p => p.FiscalYearId == fiscalYearId,
            pagingParams: null,
            cancellationToken: cancellationToken);

        var periods = periodsResult.Items
            .OrderBy(p => p.StartDate)
            .ToList();

        if (periods.Count == 0)
            throw new InvalidOperationException("FiscalYear has no FiscalPeriods. Create fiscal periods first.");

        // 2) بستن تمام دوره‌های باز (با منطق موجود)
        foreach (var p in periods.Where(x => !x.IsClosed))
            await CloseFiscalPeriodAsync(p.Id, profitAndLossAccountId, cancellationToken);

        // 3) بستن سال
        fiscalYear.IsClosed = true;
        fiscalYear.ClosedAt = DateTime.UtcNow;
        fyRepo.Update(fiscalYear);
        await uow.SaveChangesAsync(cancellationToken);

        // 4) سند افتتاحیه سال بعد
        if (!createOpeningForNextYear)
            return;

        var nextYearStart = fiscalYear.EndDate.Date.AddDays(1);

        var nextYearResult = await fyRepo.FindAsync(
            y => y.StartDate.Date == nextYearStart,
            pagingParams: null,
            cancellationToken: cancellationToken);

        var nextYear = nextYearResult.Items.FirstOrDefault();

        if (nextYear is null)
        {
            nextYear = new FiscalYear
            {
                Name = $"{fiscalYear.Name}-Next",
                StartDate = nextYearStart,
                EndDate = nextYearStart.AddYears(1).AddDays(-1),
                IsClosed = false
            };

            await fyRepo.AddAsync(nextYear, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }

        await CreateOpeningJournalForNextYearAsync(
            fiscalYearStartDate: fiscalYear.StartDate.Date,
            fiscalYearEndDate: fiscalYear.EndDate.Date,
            nextYearStartDate: nextYear.StartDate.Date,
            cancellationToken: cancellationToken);
    }

    private async Task CreateOpeningJournalForNextYearAsync(
        DateTime fiscalYearStartDate,
        DateTime fiscalYearEndDate,
        DateTime nextYearStartDate,
        CancellationToken cancellationToken)
    {
        // TrialBalance از ابتدای سال تا انتهای سال:
        // Opening* = مانده قبل از شروع سال
        // Period*  = گردش داخل سال
        // Closing* = مانده تا انتهای سال (همان چیزی که برای افتتاحیه لازم داریم)
        var tb = await reportingService.GetTrialBalanceAsync(
            fiscalYearStartDate,
            fiscalYearEndDate,
            branchId: null,
            cancellationToken: cancellationToken);

        if (tb is null || tb.Count == 0)
            return;

        // برای تشخیص نوع حساب‌ها، همه حساب‌ها را می‌گیریم
        var accountsResult = await uow.Accounts.GetAllAsync(null, cancellationToken);
        var accounts = accountsResult.Items.ToDictionary(a => a.Id);

        var openingLines = new List<JournalLine>();
        var lineNo = 1;

        foreach (var row in tb)
        {
            if (!accounts.TryGetValue(row.AccountId, out var acc))
                continue;

            // فقط ترازنامه‌ای‌ها: دارایی/بدهی/حقوق صاحبان سهام
            if (acc.Type != AccountType.Asset &&
                acc.Type != AccountType.Liability &&
                acc.Type != AccountType.Equity)
                continue;

            // مانده پایان سال:
            // اگر ClosingDebit>0 => بدهکار
            // اگر ClosingCredit>0 => بستانکار
            if (row.ClosingDebit > 0m)
            {
                openingLines.Add(new JournalLine
                {
                    LineNumber = lineNo++,
                    AccountId = row.AccountId,
                    Debit = row.ClosingDebit,
                    Credit = 0m,
                    Description = "Opening balance"
                });
            }
            else if (row.ClosingCredit > 0m)
            {
                openingLines.Add(new JournalLine
                {
                    LineNumber = lineNo++,
                    AccountId = row.AccountId,
                    Debit = 0m,
                    Credit = row.ClosingCredit,
                    Description = "Opening balance"
                });
            }
        }

        if (openingLines.Count == 0)
            return;

        var totalDebit = openingLines.Sum(x => x.Debit);
        var totalCredit = openingLines.Sum(x => x.Credit);

        // با توجه به ماهیت ترازنامه، باید تراز باشد
        if (decimal.Round(totalDebit, 2) != decimal.Round(totalCredit, 2))
            throw new InvalidOperationException("Opening journal is not balanced. Check postings and account types.");

        var openingVoucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("OpeningJournal", null, cancellationToken),
            Date = nextYearStartDate,
            Description = $"Opening balances as of {nextYearStartDate:yyyy-MM-dd}",
            Status = DocumentStatus.Posted,
            Lines = openingLines
        };

        await uow.Journals.AddAsync(openingVoucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsBalancedAsync(
        int journalId,
        CancellationToken cancellationToken = default)
    {
        var journal = await uow.Journals.GetWithLinesAsync(journalId, cancellationToken);
        if (journal is null)
            throw new InvalidOperationException($"JournalVoucher with id={journalId} not found.");

        return IsBalanced(journal);
    }

    private static bool IsBalanced(JournalVoucher journal)
    {
        var totalDebit = journal.Lines.Sum(x => x.Debit);
        var totalCredit = journal.Lines.Sum(x => x.Credit);
        return decimal.Round(totalDebit, 2) == decimal.Round(totalCredit, 2);
    }

    // ===================== Receipt =====================

    public async Task<Receipt> CreateReceiptAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await ValidateReceiptAsync(receipt, cancellationToken);

            receipt.Number = await GenerateNextNumberAsync("Receipt", receipt.BranchId, cancellationToken);
            receipt.Status = DocumentStatus.Draft;

            await uow.Receipts.AddAsync(receipt, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
            return receipt;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<Receipt?> GetReceiptAsync(
        int id,
        CancellationToken cancellationToken = default)
        => uow.Receipts.GetByIdAsync(id, cancellationToken);

    public async Task<Receipt> UpdateReceiptAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default)
    {
        var existing = await uow.Receipts.GetByIdAsync(receipt.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"Receipt with id={receipt.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted receipt cannot be updated.");

        existing.Date = receipt.Date;
        existing.PartyId = receipt.PartyId;
        existing.BranchId = receipt.BranchId;
        existing.Amount = receipt.Amount;
        existing.CurrencyId = receipt.CurrencyId;
        existing.FxRate = receipt.FxRate;
        existing.Method = receipt.Method;
        existing.BankAccountId = receipt.BankAccountId;
        existing.CashDeskCode = receipt.CashDeskCode;
        existing.ReferenceNo = receipt.ReferenceNo;
        existing.Description = receipt.Description;

        await ValidateReceiptAsync(existing, cancellationToken);

        uow.Receipts.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task PostReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var receipt = await uow.Receipts.GetByIdAsync(receiptId, cancellationToken);
            if (receipt is null)
                throw new InvalidOperationException($"Receipt with id={receiptId} not found.");

            if (receipt.Status == DocumentStatus.Posted)
                return;

            // ساخت سند حسابداری بر اساس PostingRule
            var journal = await CreateJournalForReceiptAsync(receipt, cancellationToken);

            receipt.Status = DocumentStatus.Posted;
            receipt.JournalVoucherId = journal.Id;

            uow.Receipts.Update(receipt);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ===================== Payment =====================

    public async Task<Payment> CreatePaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await ValidatePaymentAsync(payment, cancellationToken);

            payment.Number = await GenerateNextNumberAsync("Payment", payment.BranchId, cancellationToken);
            payment.Status = DocumentStatus.Draft;

            await uow.Payments.AddAsync(payment, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
            return payment;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<Payment?> GetPaymentAsync(
        int id,
        CancellationToken cancellationToken = default)
        => uow.Payments.GetByIdAsync(id, cancellationToken);

    public async Task<Payment> UpdatePaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        var existing = await uow.Payments.GetByIdAsync(payment.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException($"Payment with id={payment.Id} not found.");

        if (existing.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Posted payment cannot be updated.");

        existing.Date = payment.Date;
        existing.PartyId = payment.PartyId;
        existing.BranchId = payment.BranchId;
        existing.Amount = payment.Amount;
        existing.CurrencyId = payment.CurrencyId;
        existing.FxRate = payment.FxRate;
        existing.Method = payment.Method;
        existing.BankAccountId = payment.BankAccountId;
        existing.CashDeskCode = payment.CashDeskCode;
        existing.ReferenceNo = payment.ReferenceNo;
        existing.Description = payment.Description;

        await ValidatePaymentAsync(existing, cancellationToken);

        uow.Payments.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task PostPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = await uow.Payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment is null)
                throw new InvalidOperationException($"Payment with id={paymentId} not found.");

            if (payment.Status == DocumentStatus.Posted)
                return;

            var journal = await CreateJournalForPaymentAsync(payment, cancellationToken);

            payment.Status = DocumentStatus.Posted;
            payment.JournalVoucherId = journal.Id;

            uow.Payments.Update(payment);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ===================== Helpers =====================

    private Task ValidateReceiptAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        if (receipt.Amount <= 0)
            throw new InvalidOperationException("Receipt amount must be greater than zero.");

        if (receipt.Method == PaymentMethod.Cash &&
            string.IsNullOrWhiteSpace(receipt.CashDeskCode))
            throw new InvalidOperationException("CashDeskCode is required for cash receipt.");

        if (receipt.Method == PaymentMethod.BankTransfer &&
            receipt.BankAccountId is null)
            throw new InvalidOperationException("BankAccountId is required for bank receipt.");

        return Task.CompletedTask;
    }

    private Task ValidatePaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        if (payment.Method == PaymentMethod.Cash &&
            string.IsNullOrWhiteSpace(payment.CashDeskCode))
            throw new InvalidOperationException("CashDeskCode is required for cash payment.");

        if (payment.Method == PaymentMethod.BankTransfer &&
            payment.BankAccountId is null)
            throw new InvalidOperationException("BankAccountId is required for bank payment.");

        return Task.CompletedTask;
    }

    private async Task<JournalVoucher> CreateJournalForReceiptAsync(
        Receipt receipt,
        CancellationToken cancellationToken)
    {
        // PostingRule با DocumentType = "Receipt"
        var postingRuleRepo = uow.Repository<PostingRule>();
        var page = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "Receipt" && x.IsActive,
            null,
            cancellationToken);

        var rule = page.Items.FirstOrDefault()
                   ?? throw new InvalidOperationException("No posting rule defined for Receipt.");
        
        var period = await GetOpenFiscalPeriodAsync(receipt.Date, null, cancellationToken);

        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", receipt.BranchId, cancellationToken),
            Date = receipt.Date,
            BranchId = receipt.BranchId,
            FiscalPeriodId = period.Id,
            Description = $"Posting Receipt {receipt.Number}",
            Status = DocumentStatus.Draft
        };


        var lines = new List<JournalLine>();
        int lineNo = 1;

        // Debit: Cash/Bank (حساب نقد و بانک)
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.DebitAccountId,
            Debit = receipt.Amount,
            Credit = 0,
            RefDocumentType = "Receipt",
            RefDocumentId = receipt.Id,
            CurrencyId = receipt.CurrencyId,
            FxRate = receipt.FxRate,
            Description = $"Receipt {receipt.Number}"
        });

        // Credit: Receivable / Other
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.CreditAccountId,
            Debit = 0,
            Credit = receipt.Amount,
            RefDocumentType = "Receipt",
            RefDocumentId = receipt.Id,
            CurrencyId = receipt.CurrencyId,
            FxRate = receipt.FxRate,
            Description = $"Receipt {receipt.Number}"
        });

        voucher.Lines = lines;
        
        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    private async Task<JournalVoucher> CreateJournalForPaymentAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        var postingRuleRepo = uow.Repository<PostingRule>();
        var page = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "Payment" && x.IsActive,
            null,
            cancellationToken);

        var rule = page.Items.FirstOrDefault()
                   ?? throw new InvalidOperationException("No posting rule defined for Payment.");

        var period = await GetOpenFiscalPeriodAsync(payment.Date, null, cancellationToken);

        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", payment.BranchId, cancellationToken),
            Date = payment.Date,
            BranchId = payment.BranchId,
            FiscalPeriodId = period.Id,
            Description = $"Posting Payment {payment.Number}",
            Status = DocumentStatus.Draft
        };


        var lines = new List<JournalLine>();
        int lineNo = 1;

        // Debit: Payable / Expense
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.DebitAccountId,
            Debit = payment.Amount,
            Credit = 0,
            RefDocumentType = "Payment",
            RefDocumentId = payment.Id,
            CurrencyId = payment.CurrencyId,
            FxRate = payment.FxRate,
            Description = $"Payment {payment.Number}"
        });

        // Credit: Cash/Bank
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = rule.CreditAccountId,
            Debit = 0,
            Credit = payment.Amount,
            RefDocumentType = "Payment",
            RefDocumentId = payment.Id,
            CurrencyId = payment.CurrencyId,
            FxRate = payment.FxRate,
            Description = $"Payment {payment.Number}"
        });

        voucher.Lines = lines;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

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
    
    // ===================== Inventory Adjustment =====================

    public async Task PostInventoryAdjustmentAsync(
        int inventoryAdjustmentId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            var adjustmentRepo = uow.Repository<InventoryAdjustment>();

            var adjustment = await adjustmentRepo.GetByIdAsync(inventoryAdjustmentId, cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"InventoryAdjustment with id={inventoryAdjustmentId} not found.");

            if (adjustment.Status == DocumentStatus.Posted)
                return;

            if (!adjustment.TotalDifferenceValue.HasValue ||
                adjustment.TotalDifferenceValue.Value == 0m)
            {
                throw new InvalidOperationException(
                    "InventoryAdjustment has no TotalDifferenceValue. " +
                    "Make sure inventory adjustment is processed (and TotalDifferenceValue calculated) before posting to accounting.");
            }

            var journal = await CreateJournalForInventoryAdjustmentAsync(
                adjustment,
                cancellationToken);

            adjustment.Status = DocumentStatus.Posted;
            adjustment.JournalVoucherId = journal.Id;

            adjustmentRepo.Update(adjustment);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    private async Task<JournalVoucher> CreateJournalForInventoryAdjustmentAsync(
        InventoryAdjustment adjustment,
        CancellationToken cancellationToken)
    {
        // گرفتن PostingRule با DocumentType = "InventoryAdjustment"
        var postingRuleRepo = uow.Repository<PostingRule>();
        var page = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "InventoryAdjustment" && x.IsActive,
            null,
            cancellationToken);

        var rule = page.Items.FirstOrDefault()
                   ?? throw new InvalidOperationException(
                       "No posting rule defined for InventoryAdjustment.");

        if (!adjustment.TotalDifferenceValue.HasValue ||
            adjustment.TotalDifferenceValue.Value == 0m)
        {
            throw new InvalidOperationException(
                "TotalDifferenceValue is null or zero for InventoryAdjustment.");
        }

        var diff = adjustment.TotalDifferenceValue.Value;
        var amount = diff >= 0 ? diff : -diff;

        // منطق Debit/Credit:
        // فرض: PostingRule برای حالت افزایش موجودی تعریف شده است:
        //   diff > 0  => Debit: rule.DebitAccountId (Inventory)
        //                Credit: rule.CreditAccountId (Gain/Loss)
        //
        // اگر diff < 0 بود، حساب‌ها را برعکس می‌کنیم:
        //   diff < 0  => Debit: rule.CreditAccountId (Loss)
        //                Credit: rule.DebitAccountId (Inventory)

        int debitAccountId;
        int creditAccountId;

        if (diff > 0)
        {
            debitAccountId = rule.DebitAccountId;
            creditAccountId = rule.CreditAccountId;
        }
        else
        {
            debitAccountId = rule.CreditAccountId;
            creditAccountId = rule.DebitAccountId;
        }

        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", adjustment.BranchId, cancellationToken),
            Date = adjustment.Date,
            BranchId = adjustment.BranchId,
            Description = $"Inventory Adjustment {adjustment.Number}",
            Status = DocumentStatus.Posted
        };

        var lines = new List<JournalLine>();
        var lineNo = 1;

        // خط بدهکار
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = debitAccountId,
            Debit = amount,
            Credit = 0m,
            RefDocumentType = "InventoryAdjustment",
            RefDocumentId = adjustment.Id,
            Description = $"Inventory Adjustment {adjustment.Number}"
        });

        // خط بستانکار
        lines.Add(new JournalLine
        {
            LineNumber = lineNo++,
            AccountId = creditAccountId,
            Debit = 0m,
            Credit = amount,
            RefDocumentType = "InventoryAdjustment",
            RefDocumentId = adjustment.Id,
            Description = $"Inventory Adjustment {adjustment.Number}"
        });

        voucher.Lines = lines;

        await uow.Journals.AddAsync(voucher, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    // --------------------- Validation Helpers ---------------------

    private async Task ValidateJournalVoucherAsync(JournalVoucher voucher, CancellationToken ct)
    {
        if (voucher is null)
            throw new InvalidOperationException("Voucher is required.");

        if (voucher.Date == default)
            throw new InvalidOperationException("Voucher date is required.");

        if (voucher.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (voucher.Lines is null || voucher.Lines.Count == 0)
            throw new InvalidOperationException("Voucher must have at least one line.");

        // Line-level checks + SubLedger rules
        await ValidateJournalLinesAsync(voucher.Lines, ct);
    }

    private async Task ValidateJournalLinesAsync(IEnumerable<JournalLine> lines, CancellationToken ct)
    {
        // cache برای جلوگیری از چندبار خواندن یک Account/Party
        var accountCache = new Dictionary<int, Account>();
        var partyCache = new Dictionary<int, Core.Models.Master.Party>();

        var index = 0;
        foreach (var line in lines)
        {
            index++;

            if (line.AccountId <= 0)
                throw new InvalidOperationException($"Line[{index}]: AccountId is required.");

            if (line.Debit < 0 || line.Credit < 0)
                throw new InvalidOperationException($"Line[{index}]: Debit/Credit cannot be negative.");

            if (line.Debit > 0 && line.Credit > 0)
                throw new InvalidOperationException($"Line[{index}]: A line cannot have both Debit and Credit.");

            if (line.Debit == 0 && line.Credit == 0)
                throw new InvalidOperationException($"Line[{index}]: Either Debit or Credit must be greater than zero.");

            // -------- Account validation --------
            if (!accountCache.TryGetValue(line.AccountId, out var account))
            {
                account = await uow.Accounts.GetByIdAsync(line.AccountId, ct)
                          ?? throw new InvalidOperationException($"Line[{index}]: Account not found (Id={line.AccountId}).");
                accountCache[line.AccountId] = account;
            }

            if (!account.IsActive)
                throw new InvalidOperationException($"Line[{index}]: Account is inactive (Code={account.Code}).");

            if (!account.IsPosting)
                throw new InvalidOperationException($"Line[{index}]: Account is not posting (Code={account.Code}).");

            // -------- SubLedger rule: RequiresParty --------
            if (account.RequiresParty && line.PartyId is null)
                throw new InvalidOperationException($"Line[{index}]: Party is required for account {account.Code} - {account.Name}.");

            // -------- Party validation (if provided) --------
            if (line.PartyId is not null)
            {
                var pid = line.PartyId.Value;

                if (!partyCache.TryGetValue(pid, out var party))
                {
                    party = await uow.Parties.GetByIdAsync(pid, ct)
                            ?? throw new InvalidOperationException($"Line[{index}]: Party not found (Id={pid}).");
                    partyCache[pid] = party;
                }

                if (!party.IsActive)
                    throw new InvalidOperationException($"Line[{index}]: Party is inactive (Code={party.Code}).");

                if (account.AllowedPartyType.HasValue && party.Type != account.AllowedPartyType.Value)
                {
                    throw new InvalidOperationException(
                        $"Line[{index}]: Party type '{party.Type}' is not allowed for account {account.Code} - {account.Name}.");
                }
            }
        }
    }
}