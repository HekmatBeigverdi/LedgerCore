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
    
    // ===== گزارش‌های انبار =====

    /// <summary>
    /// مانده موجودی کالاها تا تاریخ مشخص.
    /// </summary>
    Task<IReadOnlyList<StockBalanceRowDto>> GetStockBalanceAsync(
        DateTime asOfDate,
        int? warehouseId,
        int? productId,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// کارتکس یک کالا در یک انبار (یا همه انبارها) در بازه زمانی.
    /// </summary>
    Task<IReadOnlyList<StockCardRowDto>> GetStockCardAsync(
        int productId,
        int? warehouseId,
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    // ===== گزارش‌های فروش و خرید =====

    /// <summary>
    /// فروش به تفکیک کالا در بازه زمانی.
    /// </summary>
    Task<IReadOnlyList<SalesByItemRowDto>> GetSalesByItemAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// فروش به تفکیک طرف حساب در بازه زمانی.
    /// </summary>
    Task<IReadOnlyList<SalesByPartyRowDto>> GetSalesByPartyAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// خرید به تفکیک کالا در بازه زمانی.
    /// </summary>
    Task<IReadOnlyList<PurchaseByItemRowDto>> GetPurchasesByItemAsync(
        DateTime fromDate,
        DateTime toDate,
        int? branchId,
        CancellationToken cancellationToken = default);    
}