using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Master;

public class TaxRate: AuditableEntity
{
    public string Name { get; set; } = default!;       // e.g. VAT 9%
    public decimal RatePercent { get; set; }           // 9 → یعنی 9 درصد
    public bool IsIncludedInPrice { get; set; }        // آیا در قیمت لحاظ شده؟
    public bool IsActive { get; set; } = true;
}