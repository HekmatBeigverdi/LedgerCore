using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.ViewModels.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReportsController(IReportingService reportingService) : ControllerBase
{
    // ==========================
    //  گزارش‌های مالی
    // ==========================

    /// <summary>
    /// تراز آزمایشی در بازه زمانی.
    /// GET api/reports/trial-balance?fromDate=2025-01-01&toDate=2025-01-31&branchId=1
    /// </summary>
    [HttpGet("trial-balance")]
    public async Task<ActionResult<IReadOnlyList<TrialBalanceRow>>> GetTrialBalance(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetTrialBalanceAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }

    /// <summary>
    /// دفتر کل.
    /// GET api/reports/general-ledger?fromDate=...&toDate=...&accountId=...&branchId=...
    /// </summary>
    [HttpGet("general-ledger")]
    public async Task<ActionResult<IReadOnlyList<GeneralLedgerRowDto>>> GetGeneralLedger(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? accountId,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetGeneralLedgerAsync(
            fromDate, toDate, accountId, branchId, cancellationToken);

        return Ok(data);
    }

    /// <summary>
    /// صورت سود و زیان.
    /// GET api/reports/profit-and-loss?fromDate=...&toDate=...&branchId=...
    /// </summary>
    [HttpGet("profit-and-loss")]
    public async Task<ActionResult<ProfitAndLossReportDto>> GetProfitAndLoss(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var report = await reportingService.GetProfitAndLossAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(report);
    }

    /// <summary>
    /// ترازنامه.
    /// GET api/reports/balance-sheet?asOfDate=2025-01-31&branchId=1
    /// </summary>
    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetReportDto>> GetBalanceSheet(
        [FromQuery] DateTime asOfDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var report = await reportingService.GetBalanceSheetAsync(
            asOfDate, branchId, cancellationToken);

        return Ok(report);
    }

    // ==========================
    //  گزارش‌های انبار
    // ==========================

    /// <summary>
    /// مانده موجودی کالاها تا تاریخ مشخص.
    /// GET api/reports/stock-balance?asOfDate=2025-01-31&warehouseId=&productId=&branchId=
    /// </summary>
    [HttpGet("stock-balance")]
    public async Task<ActionResult<IReadOnlyList<StockBalanceRowDto>>> GetStockBalance(
        [FromQuery] DateTime asOfDate,
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var data = await reportingService.GetStockBalanceAsync(
            asOfDate, warehouseId, productId, branchId, cancellationToken);

        return Ok(data);
    }

    /// <summary>
    /// کارتکس کالا.
    /// GET api/reports/stock-card?productId=1&warehouseId=&fromDate=...&toDate=...&branchId=
    /// </summary>
    [HttpGet("stock-card")]
    public async Task<ActionResult<IReadOnlyList<StockCardRowDto>>> GetStockCard(
        [FromQuery] int productId,
        [FromQuery] int? warehouseId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetStockCardAsync(
            productId, warehouseId, fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }

    // ==========================
    //  گزارش‌های فروش و خرید
    // ==========================

    /// <summary>
    /// فروش به تفکیک کالا.
    /// GET api/reports/sales-by-item?fromDate=...&toDate=...&branchId=
    /// </summary>
    [HttpGet("sales-by-item")]
    public async Task<ActionResult<IReadOnlyList<SalesByItemRowDto>>> GetSalesByItem(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetSalesByItemAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }

    /// <summary>
    /// فروش به تفکیک طرف حساب.
    /// GET api/reports/sales-by-party?fromDate=...&toDate=...&branchId=
    /// </summary>
    [HttpGet("sales-by-party")]
    public async Task<ActionResult<IReadOnlyList<SalesByPartyRowDto>>> GetSalesByParty(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetSalesByPartyAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }

    /// <summary>
    /// خرید به تفکیک کالا.
    /// GET api/reports/purchases-by-item?fromDate=...&toDate=...&branchId=
    /// </summary>
    [HttpGet("purchases-by-item")]
    public async Task<ActionResult<IReadOnlyList<PurchaseByItemRowDto>>> GetPurchasesByItem(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
            return BadRequest("fromDate cannot be greater than toDate.");

        var data = await reportingService.GetPurchasesByItemAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }
}