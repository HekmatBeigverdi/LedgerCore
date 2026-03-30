namespace LedgerCore.Core.ViewModels.Masters;

public class CurrencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
}