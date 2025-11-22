namespace LedgerCore.Core.ViewModels.Reports;

public class PurchaseByItemRowDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;

    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}