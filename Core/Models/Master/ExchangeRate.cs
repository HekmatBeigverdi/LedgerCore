using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class ExchangeRate: AuditableEntity
{
    public int CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public DateTime RateDate { get; set; }
    public decimal Rate { get; set; }   // چند واحد ارز پایه برای ۱ واحد این ارز
}