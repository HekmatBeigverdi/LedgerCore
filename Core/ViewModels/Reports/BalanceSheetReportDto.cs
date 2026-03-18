namespace LedgerCore.Core.ViewModels.Reports;

public class BalanceSheetReportDto
{
    public DateTime AsOfDate { get; set; }

    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }

    public IReadOnlyList<BalanceSheetLineDto> Lines { get; set; } = new List<BalanceSheetLineDto>();
}