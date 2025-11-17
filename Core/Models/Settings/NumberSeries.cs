using LedgerCore.Core.Models.Common;

namespace LedgerCore.Core.Models.Settings;

public class NumberSeries: AuditableEntity
{
    /// <summary>
    /// نوع سند (مثلاً "SalesInvoice", "PurchaseInvoice", "Journal", "Receipt")
    /// </summary>
    public string EntityType { get; set; } = default!;

    public int? BranchId { get; set; }      // در صورت نیاز سریال مختلف برای هر شعبه
    public string Prefix { get; set; } = "";       // مثال: "SI-", "PI-"
    public string? Suffix { get; set; }           // مثال: "/1403"

    public int Padding { get; set; } = 5;          // تعداد ارقام شماره (مثلاً 00001)
    public long CurrentNumber { get; set; }        // آخرین شماره استفاده شده

    public bool IsActive { get; set; } = true;
}