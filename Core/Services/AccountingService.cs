using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class AccountingService(IUnitOfWork uow) : IAccountingService
{
    // =================== Receipt ===================

    public async Task<Receipt> CreateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default)
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

    public Task<Receipt?> GetReceiptAsync(int id, CancellationToken cancellationToken = default)
        => uow.Receipts.GetByIdAsync(id, cancellationToken);

    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default)
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

    public async Task PostReceiptAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var receipt = await uow.Receipts.GetByIdAsync(receiptId, cancellationToken);
            if (receipt is null)
                throw new InvalidOperationException($"Receipt with id={receiptId} not found.");

            if (receipt.Status == DocumentStatus.Posted)
                return;

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

    // =================== Payment ===================

    public async Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
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

    public Task<Payment?> GetPaymentAsync(int id, CancellationToken cancellationToken = default)
        => uow.Payments.GetByIdAsync(id, cancellationToken);

    public async Task<Payment> UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
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

    public async Task PostPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
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

    // =================== Validate & Posting Helpers ===================

    private Task ValidateReceiptAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        if (receipt.Amount <= 0)
            throw new InvalidOperationException("Receipt amount must be greater than zero.");

        if (receipt.Method == PaymentMethod.Cash && string.IsNullOrWhiteSpace(receipt.CashDeskCode))
            throw new InvalidOperationException("CashDeskCode is required for cash receipt.");

        if (receipt.Method == PaymentMethod.BankTransfer && receipt.BankAccountId is null)
            throw new InvalidOperationException("BankAccountId is required for bank receipt.");

        return Task.CompletedTask;
    }

    private Task ValidatePaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        if (payment.Method == PaymentMethod.Cash && string.IsNullOrWhiteSpace(payment.CashDeskCode))
            throw new InvalidOperationException("CashDeskCode is required for cash payment.");

        if (payment.Method == PaymentMethod.BankTransfer && payment.BankAccountId is null)
            throw new InvalidOperationException("BankAccountId is required for bank payment.");

        return Task.CompletedTask;
    }

    private async Task<JournalVoucher> CreateJournalForReceiptAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        // بر اساس PostingRule با DocumentType = "Receipt"
        var postingRuleRepo = uow.Repository<PostingRule>();
        var rules = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "Receipt" && x.IsActive,
            null,
            cancellationToken);

        var rule = rules.Items.FirstOrDefault()
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

        // Debit: Cash/Bank
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

        // Credit: Receivable/Other
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

    private async Task<JournalVoucher> CreateJournalForPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        var postingRuleRepo = uow.Repository<PostingRule>();
        var rules = await postingRuleRepo.FindAsync(
            x => x.DocumentType == "Payment" && x.IsActive,
            null,
            cancellationToken);

        var rule = rules.Items.FirstOrDefault()
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

        // Debit: Payable/Expense
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

    private async Task<string> GenerateNextNumberAsync(string entityType, int? branchId, CancellationToken cancellationToken)
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

    // ===== متدهای دیگر IAccountingService (Journal, ClosePeriod, ...) را هم اینجا پیاده کن =====
    public Task<JournalVoucher> CreateJournalAsync(JournalVoucher voucher, CancellationToken cancellationToken = default)
    {
        // TODO: پیاده‌سازی طبق نیاز خودت
        throw new NotImplementedException();
    }

    public Task<JournalVoucher?> GetJournalAsync(int id, CancellationToken cancellationToken = default)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task PostJournalAsync(int journalId, CancellationToken cancellationToken = default)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task CloseFiscalPeriodAsync(int fiscalPeriodId, CancellationToken cancellationToken = default)
    {
        // TODO
        throw new NotImplementedException();
    }

    public Task<bool> IsBalancedAsync(int journalId, CancellationToken cancellationToken = default)
    {
        // TODO
        throw new NotImplementedException();
    }
}