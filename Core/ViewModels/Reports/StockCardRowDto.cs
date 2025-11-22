namespace LedgerCore.Core.ViewModels.Reports;

public class StockCardRowDto
{
    public DateTime Date { get; set; }
    public string DocumentType { get; set; } = default!;
    public string DocumentNumber { get; set; } = default!;
    public string? Description { get; set; }

    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public decimal InQuantity { get; set; }
    public decimal OutQuantity { get; set; }

    public decimal BalanceQuantity { get; set; }

    public decimal? UnitPrice { get; set; }
    public decimal? TotalAmount { get; set; }
}