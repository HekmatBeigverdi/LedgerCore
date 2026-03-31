namespace LedgerCore.Core.ViewModels.Masters;

public class ExchangeRateDto
{
    public int Id { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public DateTime RateDate { get; set; }
    public decimal Rate { get; set; }
}