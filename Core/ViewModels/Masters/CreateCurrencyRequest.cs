namespace LedgerCore.Core.ViewModels.Masters;

public class CreateCurrencyRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsBaseCurrency { get; set; }
}