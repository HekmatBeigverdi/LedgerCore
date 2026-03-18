using LedgerCore.Core.Constants;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.Models.Settings;

namespace LedgerCore.Core.Services;

public class PayrollService(
    IUnitOfWork uow,
    ICurrentBranchService currentBranch,
    INumberSeriesService numberSeries,
    IPostingEngineService postingEngine) : IPayrollService
{
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();
    public async Task<PayrollDocument> CalculatePayrollAsync(
        PayrollDocument payroll,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentBranchId = currentBranch.GetRequiredBranchId();

            if (payroll.BranchId == 0)
                payroll.BranchId = currentBranchId;
            else if (payroll.BranchId != currentBranchId)
                throw new InvalidOperationException("BranchId is not valid for current branch scope.");
            
            // اگر جدید است، شماره بده
            if (string.IsNullOrWhiteSpace(payroll.Number))
            {
                payroll.Number = await numberSeries.NextAsync(
                    NumberSeriesKeys.Payroll,
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
            var branchId = currentBranch.GetRequiredBranchId();

            var payroll = await uow.Payrolls.GetWithLinesAsync(payrollDocumentId, branchId, cancellationToken)
                          ?? throw new InvalidOperationException($"PayrollDocument with id={payrollDocumentId} not found.");

            if (payroll.Status == PayrollStatus.Posted)
                return;

            if (payroll.Status != PayrollStatus.Calculated &&
                payroll.Status != PayrollStatus.Approved)
                throw new InvalidOperationException("Only calculated or approved payroll can be posted.");

            if (payroll.TotalGross <= 0)
                throw new InvalidOperationException("Payroll totals are invalid.");

            var context = new PostingContext
            {
                Gross = payroll.TotalGross,
                TotalDeductions = payroll.TotalDeductions,
                TotalNet = payroll.TotalNet,
                Description = $"Payroll {payroll.Number} for period {payroll.PayrollPeriod?.Code}"
            };

            var journal = await postingEngine.BuildJournalAsync(
                documentType: "Payroll",
                branchId: payroll.BranchId,
                date: payroll.Date,
                refDocumentId: payroll.Id,
                refDocumentNumber: payroll.Number,
                context: context,
                cancellationToken: cancellationToken);

            await uow.Journals.AddAsync(journal, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            payroll.Status = PayrollStatus.Posted;
            payroll.JournalVoucher = journal;

            uow.Payrolls.Update(payroll);
            await uow.SaveChangesAsync(cancellationToken);

            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;        }
    }
    
}