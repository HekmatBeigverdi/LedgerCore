using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class SalesInvoice: AuditableEntity
{
    public string Number { get; set; } = default!;           // شماره فاکتور
    public DateTime Date { get; set; } = DateTime.UtcNow;    // تاریخ فاکتور
    public DateTime? DueDate { get; set; }                   // سررسید (برای فروش اعتباری)

    public int CustomerId { get; set; }
    public Party? Customer { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? WarehouseId { get; set; }                    // انبار خروج کالا
    public Warehouse? Warehouse { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;                // نرخ تبدیل به ارز پایه

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public decimal TotalNetAmount { get; set; }              // جمع خالص (جمع NetAmount سطرها)
    public decimal TotalDiscount { get; set; }               // جمع تخفیف‌ها
    public decimal TotalTaxAmount { get; set; }              // جمع مالیات‌ها
    public decimal TotalAmount { get; set; }                 // مبلغ نهایی پرداختی

    public bool IsCashSale { get; set; } = false;            // نقدی یا اعتباری؟

    // ارتباط با سند حسابداری پست‌شده:
    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}