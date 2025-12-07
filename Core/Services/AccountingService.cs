using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class AccountingService(IUnitOfWork uow) : IAccountingService
{
    // ===================== ژورنال =====================

    public async Task<JournalVoucher> CreateJournalAsync(
        JournalVoucher voucher,
        CancellationToken cancellationToken = default)
    {
        // اگر شماره ندارد، از NumberSeries بگیریم
        if (string.IsNullOrWhiteSpace(voucher.Number))
        {
            voucher.Number = await GenerateNextNumberAsync("Journal", voucher.BranchId, cancellationToken);
        }

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

    public async Task CloseFiscalPeriodAsync(
        int fiscalPeriodId,
        CancellationToken cancellationToken = default)
    {
        var periodRepo = uow.Repository<FiscalPeriod>();
        var period = await periodRepo.GetByIdAsync(fiscalPeriodId, cancellationToken)
                     ?? throw new InvalidOperationException($"FiscalPeriod with id={fiscalPeriodId} not found.");

        if (period.IsClosed)
            return;

        // اینجا می‌توانی قوانین بستن دوره (ثبت سند اختتامیه و افتتاحیه و ...) را اضافه کنی
        // فعلاً ساده: فقط flag بستن را می‌زنیم
        period.IsClosed = true;
        period.ClosedAt = DateTime.UtcNow;

        periodRepo.Update(period);
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

        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", receipt.BranchId, cancellationToken),
            Date = receipt.Date,
            BranchId = receipt.BranchId,
            Description = $"Receipt {receipt.Number}",
            Status = DocumentStatus.Posted
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

        var voucher = new JournalVoucher
        {
            Number = await GenerateNextNumberAsync("Journal", payment.BranchId, cancellationToken),
            Date = payment.Date,
            BranchId = payment.BranchId,
            Description = $"Payment {payment.Number}",
            Status = DocumentStatus.Posted
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

    
}