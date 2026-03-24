namespace LedgerCore.Core.ViewModels.Masters;

public class TaxRateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal RatePercent { get; set; }
    public bool IsIncludedInPrice { get; set; }
    public bool IsActive { get; set; }
}