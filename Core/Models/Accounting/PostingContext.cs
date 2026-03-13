namespace LedgerCore.Core.Models.Accounting;

public class PostingContext
{
    public decimal Total { get; set; }
    public decimal Net { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Gross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public decimal DifferenceValue { get; set; }

    public int? PartyId { get; set; }
    public int? CurrencyId { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public string? Description { get; set; }
}