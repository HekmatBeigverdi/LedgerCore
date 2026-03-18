using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Master;

namespace LedgerCore.Core.Models.Documents;

public class InvoiceLine: BaseEntity
{
    public int LineNumber { get; set; }             // شماره ردیف برای مرتب‌سازی
    public string? Description { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }           // مبلغ تخفیف روی این ردیف

    public int? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }

    public decimal NetAmount { get; set; }          // مبلغ قبل از مالیات (Qty * Price - Discount)
    public decimal TaxAmount { get; set; }          // مبلغ مالیات این ردیف
    public decimal TotalAmount { get; set; }        // NetAmount + TaxAmount

    // ارتباط با فاکتور فروش / خرید:
    public int? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public int? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
}