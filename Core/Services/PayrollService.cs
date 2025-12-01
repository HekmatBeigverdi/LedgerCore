using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class PayrollService(IUnitOfWork uow) : IPayrollService
{
    public async Task<PayrollDocument> CalculatePayrollAsync(
        PayrollDocument payroll,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // اگر جدید است، شماره بده
            if (string.IsNullOrWhiteSpace(payroll.Number))
            {
                payroll.Number = await GenerateNextNumberAsync(
                    "Payroll",
                    payroll.BranchId,
                    cancellationToken);
            }

            // محاسبه Net و جمع‌ها
            foreach (var line in payroll.Lines)
            {
                line.NetAmount = line.GrossAmount - line.Deductions;
                if (line.NetAmount < 0)
                    throw new InvalidOperationException("NetAmount cannot be negative.");
            }

            payroll.TotalGross = payroll.Lines.Sum(x => x.GrossAmount);
            payroll.TotalDeductions = payroll.Lines.Sum(x => x.Deductions);
            payroll.TotalNet = payroll.Lines.Sum(x => x.NetAmount);

            payroll.Status = PayrollStatus.Calculated;

            if (payroll.Id == 0)
            {
                await uow.Payrolls.AddAsync(payroll, cancellationToken);
            }
            else
            {
                uow.Payrolls.Update(payroll);
            }

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);

            return payroll;
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task PostPayrollAsync(
        int payrollDocumentId,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var payroll = await uow.Payrolls.GetWithLinesAsync(payrollDocumentId, cancellationToken)
                          ?? throw new InvalidOperationException($"PayrollDocument with id={payrollDocumentId} not found.");

            if (payroll.Status == PayrollStatus.Posted)
                return;

            if (payroll.Status != PayrollStatus.Calculated &&
                payroll.Status != PayrollStatus.Approved)
                throw new InvalidOperationException("Only calculated or approved payroll can be posted.");

            if (payroll.TotalGross <= 0)
                throw new InvalidOperationException("Payroll totals are invalid.");

            // خواندن PostingRule مربوط به Payroll
            var postingRuleRepo = uow.Repository<PostingRule>();
            var prPage = await postingRuleRepo.FindAsync(
                x => x.DocumentType == "Payroll" && x.IsActive,
                null,
                cancellationToken);

            var rule = prPage.Items.FirstOrDefault()
                       ?? throw new InvalidOperationException("No posting rule defined for Payroll.");

            // ساخت سند حسابداری
            var journal = new JournalVoucher
            {
                Number = await GenerateNextNumberAsync("Journal", payroll.BranchId, cancellationToken),
                Date = payroll.Date,
                BranchId = payroll.BranchId,
                Description = $"Payroll {payroll.Number} for period {payroll.PayrollPeriod?.Code}",
                Status = DocumentStatus.Posted
            };

            var lines = new List<JournalLine>();
            var lineNo = 1;

            // 1) Debit: هزینه حقوق (Gross)
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = rule.DebitAccountId,
                Debit = payroll.TotalGross,
                Credit = 0,
                RefDocumentType = "Payroll",
                RefDocumentId = payroll.Id,
                Description = $"Payroll gross expense {payroll.Number}"
            });

            // 2) Credit: حقوق پرداختنی (Net)
            lines.Add(new JournalLine
            {
                LineNumber = lineNo++,
                AccountId = rule.CreditAccountId,
                Debit = 0,
                Credit = payroll.TotalNet,
                RefDocumentType = "Payroll",
                RefDocumentId = payroll.Id,
                Description = $"Payroll net payable {payroll.Number}"
            });

            // 3) Credit: کسورات (اگر حساب Tax برای این کار استفاده شود)
            if (payroll.TotalDeductions > 0 && rule.TaxAccountId.HasValue)
            {
                lines.Add(new JournalLine
                {
                    LineNumber = lineNo++,
                    AccountId = rule.TaxAccountId.Value,
                    Debit = 0,
                    Credit = payroll.TotalDeductions,
                    RefDocumentType = "Payroll",
                    RefDocumentId = payroll.Id,
                    Description = $"Payroll deductions {payroll.Number}"
                });
            }

            journal.Lines = lines;

            await uow.Journals.AddAsync(journal, cancellationToken);

            // بروزرسانی سند حقوق
            payroll.Status = PayrollStatus.Posted;
            payroll.JournalVoucher = journal;

            uow.Payrolls.Update(payroll);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ===== Helper: NumberSeries =====

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
}