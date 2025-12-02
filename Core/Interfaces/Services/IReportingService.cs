using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.ViewModels.Reports;

namespace LedgerCore.Core.Interfaces.Services;

public interface IReportingService
{
    /// <summary>
    /// تراز آزمایشی بین دو تاریخ (بر اساس سندهای پست شده)
    /// </summary>
    Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// دفتر کل یک حساب (یا همه حساب‌ها) در بازه زمانی
    /// </summary>
    Task<IReadOnlyList<GeneralLedgerRowDto>> GetGeneralLedgerAsync(
        DateTime fromDate,
        DateTime toDate,
        int? accountId,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// صورت سود و زیان برای بازه زمانی
    /// </summary>
    Task<ProfitAndLossReportDto> GetProfitAndLossAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ترازنامه در تاریخ مشخص
    /// </summary>
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(
        DateTime asOfDate,
        int? branchId,
        CancellationToken cancellationToken = default);
}