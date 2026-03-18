namespace LedgerCore.Core.ViewModels.Reports;

public class StockBalanceRowDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;

    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public decimal QuantityOnHand { get; set; }
}