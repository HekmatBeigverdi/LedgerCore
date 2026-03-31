namespace LedgerCore.Core.ViewModels.Masters;

public class CreateExchangeRateRequest
{
    public int CurrencyId { get; set; }
    public DateTime RateDate { get; set; }
    public decimal Rate { get; set; }
}