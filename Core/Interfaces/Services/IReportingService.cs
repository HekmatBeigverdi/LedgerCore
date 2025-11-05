using LedgerCore.Core.Models.Accounting;

namespace LedgerCore.Core.Interfaces.Services;

public interface IReportingService
{
    Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    // می‌توانی متدهای دیگر مثل BalanceSheet, ProfitAndLoss اضافه کنی
}