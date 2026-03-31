namespace LedgerCore.Core.ViewModels.Masters;

public class UpdateCurrencyRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
}