using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.ViewModels.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReportsController(IReportingService reportingService) : ControllerBase
{
    // GET api/v1/reports/trial-balance?fromDate=2025-01-01&toDate=2025-01-31&branchId=1
    [HttpGet("trial-balance")]
    public async Task<ActionResult<IReadOnlyList<TrialBalanceRow>>> GetTrialBalance(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var data = await reportingService.GetTrialBalanceAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(data);
    }

    // GET api/v1/reports/general-ledger?fromDate=...&toDate=...&accountId=...&branchId=...
    [HttpGet("general-ledger")]
    public async Task<ActionResult<IReadOnlyList<GeneralLedgerRowDto>>> GetGeneralLedger(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? accountId,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var data = await reportingService.GetGeneralLedgerAsync(
            fromDate, toDate, accountId, branchId, cancellationToken);

        return Ok(data);
    }

    // GET api/v1/reports/profit-and-loss?fromDate=...&toDate=...&branchId=...
    [HttpGet("profit-and-loss")]
    public async Task<ActionResult<ProfitAndLossReportDto>> GetProfitAndLoss(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var report = await reportingService.GetProfitAndLossAsync(
            fromDate, toDate, branchId, cancellationToken);

        return Ok(report);
    }

    // GET api/v1/reports/balance-sheet?asOfDate=...&branchId=...
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
}