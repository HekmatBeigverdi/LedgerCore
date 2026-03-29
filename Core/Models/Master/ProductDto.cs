namespace LedgerCore.Core.Models.Master;

public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int? CategoryId { get; set; }
    public string? Barcode { get; set; }
    public bool IsService { get; set; }
    public decimal? DefaultSalesPrice { get; set; }
    public decimal? DefaultPurchasePrice { get; set; }
    public int? DefaultTaxRateId { get; set; }
    public bool IsActive { get; set; }
}