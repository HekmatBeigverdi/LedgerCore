using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class Currency: AuditableEntity
{
    public string Code { get; set; } = default!;   // IRR, USD, EUR
    public string Name { get; set; } = default!;   // Iranian Rial, US Dollar
    public int DecimalPlaces { get; set; } = 2;    // تعداد اعشار
    public bool IsBaseCurrency { get; set; }       // ارز پایه سیستم
        = false;
}