namespace LedgerCore.Core.ViewModels.Documents;

public class CreatePurchaseInvoiceRequest
{
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public int SupplierId { get; set; }

    public int? BranchId { get; set; }
    public int? WarehouseId { get; set; }

    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public List<CreatePurchaseInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreatePurchaseInvoiceLineRequest
{
    public int LineNumber { get; set; }
    public string? Description { get; set; }

    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }

    public int? TaxRateId { get; set; }
}