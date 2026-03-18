namespace LedgerCore.Core.ViewModels.Dashboard;

public class DailySalesTrendDto
{
    public DateTime Date { get; set; }

    public int SalesInvoiceCount { get; set; }
    public decimal SalesTotal { get; set; }
}