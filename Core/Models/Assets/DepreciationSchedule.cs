using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Assets;

public class DepreciationSchedule: AuditableEntity
{
    public int FixedAssetId { get; set; }
    public FixedAsset? FixedAsset { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal DepreciationAmount { get; set; }        // میزان استهلاک این دوره
    public decimal AccumulatedDepreciation { get; set; }   // استهلاک انباشته پس از این دوره
    public decimal NetBookValue { get; set; }              // ارزش دفتری پس از این دوره

    /// <summary>
    /// اگر برای این دوره سند حسابداری ثبت شده باشد، آیدی آن
    /// </summary>
    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }

    public bool IsPosted { get; set; } = false;            // آیا به حسابداری ارسال شده؟
}