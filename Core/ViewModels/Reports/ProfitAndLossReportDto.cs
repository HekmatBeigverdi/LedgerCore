namespace LedgerCore.Core.ViewModels.Reports;

public class ProfitAndLossReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }

    /// <summary>
    /// سود (مقدار مثبت) یا زیان (مقدار منفی)
    /// </summary>
    public decimal NetProfitOrLoss { get; set; }

    public IReadOnlyList<ProfitAndLossLineDto> Lines { get; set; } = new List<ProfitAndLossLineDto>();
}