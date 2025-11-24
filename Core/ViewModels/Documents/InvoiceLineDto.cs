namespace LedgerCore.Core.ViewModels.Documents;

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public string? Description { get; set; }

    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }

    public int? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}