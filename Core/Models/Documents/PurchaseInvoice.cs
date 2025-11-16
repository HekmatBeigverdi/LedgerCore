using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class PurchaseInvoice: AuditableEntity
{
    public string Number { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public int SupplierId { get; set; }
    public Party? Supplier { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int? WarehouseId { get; set; }                    // انبار ورود کالا
    public Warehouse? Warehouse { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal FxRate { get; set; } = 1m;

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public decimal TotalNetAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // ارتباط با سند حسابداری پست‌شده:
    public int? JournalVoucherId { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}